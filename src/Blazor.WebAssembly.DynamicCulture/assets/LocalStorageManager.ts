export function getBlazorCulture(): string | null {
    return window.localStorage.getItem("BlazorCulture");
}

export function setBlazorCulture(value: string): void {
    window.localStorage.setItem("BlazorCulture", value);
}
