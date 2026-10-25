// Each .NET runtime has its own boot configuration and managed assembly registry.
const runtimeLoads = new WeakMap();

// .NET 6/7 replace readSatelliteAssemblies after every download. Unmarshalled
// interop caches bound functions, so call through stable wrappers to read the latest batch.
const legacyLoader = {
    getSatelliteAssemblies: cultures => globalThis.Blazor._internal.getSatelliteAssemblies(cultures),
    readSatelliteAssemblies: () => globalThis.Blazor._internal.readSatelliteAssemblies()
};

export function initializeLegacy() {
    globalThis.BlazorDynamicCultureLoader = legacyLoader;
}

export async function loadSatelliteAssemblies(runtime, cultures) {
    const resources = runtime.getConfig().resources;
    const satellites = resources?.satelliteResources;
    if (!satellites) {
        return;
    }

    let loads = runtimeLoads.get(runtime);
    if (!loads) {
        loads = new Map();
        runtimeLoads.set(runtime, loads);
    }

    const pending = [];
    for (const culture of new Set(cultures)) {
        if (!Object.hasOwn(satellites, culture)) {
            continue;
        }

        const entries = satellites[culture];
        // .NET 8/9 use dictionaries; .NET 10 uses arrays of mutable download assets.
        const assets = Array.isArray(entries)
            ? entries.map(asset => [asset.name, asset])
            : Object.entries(entries);

        for (const [name, entry] of assets) {
            const key = `${culture}/${name}`;
            let load = loads.get(key);
            if (!load) {
                let isolated;
                if (Array.isArray(entries)) {
                    const asset = { ...entry };
                    delete asset.pendingDownload;
                    delete asset.pendingDownloadInternal;
                    delete asset.buffer;
                    isolated = [asset];
                } else {
                    isolated = { [name]: entry };
                }

                // The native loader eagerly captures its assets before returning its promise.
                // Restore the framework's manifest synchronously, before downloads or other callers resume.
                let operation;
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
    if (failure) {
        throw failure.reason;
    }
}
