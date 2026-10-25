import { describe, expect, test } from "bun:test";
import { initializeLegacy, loadSatelliteAssemblies } from "../../src/Blazor.WebAssembly.DynamicCulture.Loader/assets/SatelliteAssemblies";

function runtime(satellites: Record<string, unknown>, loader: (assets: any, cultures: string[]) => Promise<void>) {
    const resources = { satelliteResources: satellites };
    return {
        getConfig: () => ({ resources }),
        INTERNAL: { loadSatelliteAssemblies: (cultures: string[]) => loader(resources.satelliteResources, cultures) }
    };
}

describe("isolated satellite downloads", () => {
    test("legacy interop reads the latest batch even when its wrapper is cached", () => {
        const host = globalThis as any;
        const previousBlazor = host.Blazor;
        const previousLoader = host.BlazorDynamicCultureLoader;
        try {
            host.Blazor = { _internal: { getSatelliteAssemblies: (cultures: string[]) => cultures, readSatelliteAssemblies: () => "first" } };
            initializeLegacy();
            expect(host.BlazorDynamicCultureLoader.getSatelliteAssemblies(["fr"])).toEqual(["fr"]);
            const boundRead = host.BlazorDynamicCultureLoader.readSatelliteAssemblies.bind(host.BlazorDynamicCultureLoader);
            expect(boundRead()).toBe("first");
            host.Blazor._internal.readSatelliteAssemblies = () => "second";
            expect(boundRead()).toBe("second");
        } finally {
            if (previousBlazor === undefined) delete host.Blazor;
            else host.Blazor = previousBlazor;
            if (previousLoader === undefined) delete host.BlazorDynamicCultureLoader;
            else host.BlazorDynamicCultureLoader = previousLoader;
        }
    });

    test("preserves framework assets, including responses consumed by a previous native load", async () => {
        const response = new Response("satellite");
        await response.arrayBuffer();
        const asset = { name: "Translations.wasm", hash: "hash", virtualPath: "Translations.dll", pendingDownload: Promise.resolve(response), pendingDownloadInternal: { response }, buffer: new ArrayBuffer(1) };
        const satellites = { fr: [asset] };
        let calls = 0;
        const instance = runtime(satellites, async (isolated, cultures) => {
            calls++;
            expect(cultures).toEqual(["fr"]);
            expect(isolated.fr[0]).not.toBe(asset);
            expect(isolated.fr[0]).toEqual({ name: "Translations.wasm", hash: "hash", virtualPath: "Translations.dll" });
            await Promise.resolve();
            expect(instance.getConfig().resources.satelliteResources).toBe(satellites);
        });

        await loadSatelliteAssemblies(instance, ["fr", "fr"]);
        await loadSatelliteAssemblies(instance, ["fr"]);

        expect(calls).toBe(1);
        expect(asset.pendingDownloadInternal.response).toBe(response);
        expect(instance.getConfig().resources.satelliteResources).toBe(satellites);
    });

    test("does not call the native loader when resources or satellites are absent", async () => {
        for (const config of [{}, { resources: {} }]) {
            const instance = {
                getConfig: () => config,
                INTERNAL: { loadSatelliteAssemblies: () => { throw new Error("No satellites exist"); } }
            };
            await loadSatelliteAssemblies(instance, ["fr"]);
        }
    });

    test("empty cultures and empty manifests do not call the native loader", async () => {
        const instance = runtime({ fr: [], en: {} }, () => { throw new Error("No assets requested"); });
        await loadSatelliteAssemblies(instance, []);
        await loadSatelliteAssemblies(instance, ["fr", "en", "missing"]);
    });

    test("coalesces concurrent calls and keeps runtime instances independent", async () => {
        let calls = 0;
        let complete!: () => void;
        const gate = new Promise<void>(resolve => { complete = resolve; });
        const satellites = { fr: [{ name: "Translations.wasm" }] };
        const loader = async () => { calls++; await gate; };
        const first = runtime(satellites, loader);
        const second = runtime(satellites, loader);
        const pending = [loadSatelliteAssemblies(first, ["fr"]), loadSatelliteAssemblies(first, ["fr"]), loadSatelliteAssemblies(second, ["fr"])];
        expect(calls).toBe(2);
        complete();
        await Promise.all(pending);
    });

    test("retries only failed assets after draining successful registrations", async () => {
        const attempts = new Map<string, number>();
        const satellites = { fr: [{ name: "Good.wasm" }, { name: "Retry.wasm" }] };
        let goodCompleted = false;
        const instance = runtime(satellites, async isolated => {
            const name = isolated.fr[0].name;
            const attempt = (attempts.get(name) || 0) + 1;
            attempts.set(name, attempt);
            if (name === "Retry.wasm" && attempt === 1) throw new Error("transient");
            await new Promise(resolve => setTimeout(resolve, 10));
            if (name === "Good.wasm") goodCompleted = true;
        });

        await expect(loadSatelliteAssemblies(instance, ["fr"])).rejects.toThrow("transient");
        expect(goodCompleted).toBe(true);
        await loadSatelliteAssemblies(instance, ["fr"]);

        expect(attempts.get("Good.wasm")).toBe(1);
        expect(attempts.get("Retry.wasm")).toBe(2);
        expect(instance.getConfig().resources.satelliteResources).toBe(satellites);
    });

    test("restores the manifest on synchronous failure and allows retry", async () => {
        const satellites = { fr: [{ name: "Translations.wasm" }] };
        let fail = true;
        const instance = runtime(satellites, () => {
            if (fail) throw new Error("failed synchronously");
            return Promise.resolve();
        });
        await expect(loadSatelliteAssemblies(instance, ["fr"])).rejects.toThrow("failed synchronously");
        expect(instance.getConfig().resources.satelliteResources).toBe(satellites);
        fail = false;
        await loadSatelliteAssemblies(instance, ["fr"]);
    });

    test("supports dictionary manifests without losing sibling assets", async () => {
        const satellites = { fr: { "First.wasm": "hash1", "Second.wasm": "hash2" } };
        const seen: unknown[] = [];
        const instance = runtime(satellites, async isolated => { seen.push(isolated); });
        await loadSatelliteAssemblies(instance, ["fr", "missing", "toString"]);
        expect(seen).toEqual([{ fr: { "First.wasm": "hash1" } }, { fr: { "Second.wasm": "hash2" } }]);
        expect(instance.getConfig().resources.satelliteResources).toBe(satellites);
    });
});
