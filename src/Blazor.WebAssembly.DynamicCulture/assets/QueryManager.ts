export function getQueryValue(key: string): string | null {
    return new URLSearchParams(window.location.search).get(key);
}
