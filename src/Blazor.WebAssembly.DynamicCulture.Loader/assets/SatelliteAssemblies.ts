interface SatelliteAsset {
    name: string;
    pendingDownload?: unknown;
    pendingDownloadInternal?: unknown;
    buffer?: unknown;
    [property: string]: unknown;
}

type CultureAssets = SatelliteAsset[] | Record<string, string>;
type SatelliteResources = Record<string, CultureAssets>;

interface SatelliteRuntime {
    getConfig(): { resources?: { satelliteResources?: SatelliteResources } };
    INTERNAL: { loadSatelliteAssemblies(cultures: string[]): Promise<void> };
}

// Unmarshalled interop passes managed arrays and objects as numeric handles.
interface LegacySatelliteLoader {
    getSatelliteAssemblies(cultures: number): number;
    readSatelliteAssemblies(): number;
}

declare global {
    var Blazor: { _internal: LegacySatelliteLoader };
    var BlazorDynamicCultureLoader: LegacySatelliteLoader;
}

// Each .NET runtime has its own boot configuration and managed assembly registry.
const runtimeLoads = new WeakMap<SatelliteRuntime, Map<string, Promise<void>>>();

// .NET 6/7 replace readSatelliteAssemblies after every download. Unmarshalled
// interop caches bound functions, so call through stable wrappers to read the latest batch.
const legacyLoader: LegacySatelliteLoader = {
    getSatelliteAssemblies: cultures => globalThis.Blazor._internal.getSatelliteAssemblies(cultures),
    readSatelliteAssemblies: () => globalThis.Blazor._internal.readSatelliteAssemblies()
};

export function initializeLegacy(): void {
    globalThis.BlazorDynamicCultureLoader = legacyLoader;
}

export async function loadSatelliteAssemblies(runtime: SatelliteRuntime, cultures: string[]): Promise<void> {
    const resources = runtime.getConfig().resources;
    if (!resources?.satelliteResources) {
        return;
    }
    const satellites = resources.satelliteResources;

    let loads = runtimeLoads.get(runtime);
    if (!loads) {
        loads = new Map<string, Promise<void>>();
        runtimeLoads.set(runtime, loads);
    }

    const pending: Promise<void>[] = [];
    for (const culture of new Set(cultures)) {
        if (!Object.hasOwn(satellites, culture)) {
            continue;
        }

        const entries = satellites[culture];
        // .NET 8/9 use dictionaries; .NET 10 uses arrays of mutable download assets.
        const assets: Array<[string, SatelliteAsset | string]> = Array.isArray(entries)
            ? entries.map(asset => [asset.name, asset])
            : Object.entries(entries);

        for (const [name, entry] of assets) {
            const key = `${culture}/${name}`;
            let load = loads.get(key);
            if (!load) {
                let isolated: CultureAssets;
                if (typeof entry === "string") {
                    isolated = { [name]: entry };
                } else {
                    const asset = { ...entry };
                    delete asset.pendingDownload;
                    delete asset.pendingDownloadInternal;
                    delete asset.buffer;
                    isolated = [asset];
                }

                // The native loader eagerly captures its assets before returning its promise.
                // Restore the framework's manifest synchronously, before downloads or other callers resume.
                let operation: Promise<void>;
                try {
                    resources.satelliteResources = { [culture]: isolated };
                    operation = runtime.INTERNAL.loadSatelliteAssemblies([culture]);
                } catch (error) {
                    operation = Promise.reject(error);
                } finally {
                    resources.satelliteResources = satellites;
                }

                load = Promise.resolve(operation).catch(error => {
                    loads.delete(key);
                    throw error;
                });
                loads.set(key, load);
            }
            pending.push(load);
        }
    }

    // Drain every operation before reporting failure, so a retry cannot overlap unfinished registrations.
    const results = await Promise.allSettled(pending);
    const failure = results.find(result => result.status === "rejected");
    if (failure?.status === "rejected") {
        throw failure.reason;
    }
}
