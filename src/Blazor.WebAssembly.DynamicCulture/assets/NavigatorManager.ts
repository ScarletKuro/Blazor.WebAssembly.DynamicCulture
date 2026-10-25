export function navigatorLanguage(): Promise<string> {
    return Promise.resolve(navigator.language);
}

export function navigatorLanguages(): Promise<readonly string[]> {
    return Promise.resolve(navigator.languages);
}
