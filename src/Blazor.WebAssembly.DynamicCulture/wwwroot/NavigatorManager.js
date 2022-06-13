export function navigatorLanguage() {
    return Promise.resolve(navigator.language);
};

export function navigatorLanguages() {
    return Promise.resolve(navigator.languages);
};