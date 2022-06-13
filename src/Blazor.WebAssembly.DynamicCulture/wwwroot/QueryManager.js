export function getQueryValue(key) {
    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);
    const keyParam = urlParams.get(key);
    return keyParam;
};