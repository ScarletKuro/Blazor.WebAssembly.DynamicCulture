export function getBlazorCulture() {
    return window.localStorage['BlazorCulture'];
};
export function setBlazorCulture(value) {
    window.localStorage['BlazorCulture'] = value;
};