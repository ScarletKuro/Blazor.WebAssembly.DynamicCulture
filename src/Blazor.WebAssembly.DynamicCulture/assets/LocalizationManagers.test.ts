import { afterEach, beforeEach, describe, expect, test } from "bun:test";
import { getBlazorCulture, setBlazorCulture } from "./LocalStorageManager";
import { navigatorLanguage, navigatorLanguages } from "./NavigatorManager";
import { getQueryValue } from "./QueryManager";

type TestWindow = {
    location: { search: string };
    localStorage: Pick<Storage, "getItem" | "setItem">;
};

let previousWindow: PropertyDescriptor | undefined;
let previousNavigator: PropertyDescriptor | undefined;
let browserWindow: TestWindow;
let browserNavigator: { language: string; languages: readonly string[] };
let storedValues: Map<string, string>;

beforeEach(() => {
    previousWindow = Object.getOwnPropertyDescriptor(globalThis, "window");
    previousNavigator = Object.getOwnPropertyDescriptor(globalThis, "navigator");
    storedValues = new Map();
    browserWindow = {
        location: { search: "" },
        localStorage: {
            getItem(key) {
                return storedValues.get(key) ?? null;
            },
            setItem(key, value) {
                storedValues.set(key, String(value));
            }
        }
    };
    browserNavigator = { language: "en-US", languages: ["en-US", "ja-JP"] };

    Object.defineProperty(globalThis, "window", { configurable: true, value: browserWindow });
    Object.defineProperty(globalThis, "navigator", { configurable: true, value: browserNavigator });
});

afterEach(() => {
    if (previousWindow) {
        Object.defineProperty(globalThis, "window", previousWindow);
    } else {
        Reflect.deleteProperty(globalThis, "window");
    }

    if (previousNavigator) {
        Object.defineProperty(globalThis, "navigator", previousNavigator);
    } else {
        Reflect.deleteProperty(globalThis, "navigator");
    }
});

describe("local storage culture", () => {
    test("returns null when no culture has been saved", () => {
        expect(getBlazorCulture()).toBeNull();
    });

    test.each(["fr-FR", "ja-JP", ""])('round-trips the culture "%s"', (culture) => {
        setBlazorCulture(culture);

        expect(getBlazorCulture()).toBe(culture);
        expect(storedValues.get("BlazorCulture")).toBe(culture);
    });

    test("reads a culture saved before the module was used", () => {
        storedValues.set("BlazorCulture", "ru-RU");

        expect(getBlazorCulture()).toBe("ru-RU");
    });

    test("replaces the previous culture without changing unrelated storage", () => {
        storedValues.set("theme", "dark");
        setBlazorCulture("en-US");
        setBlazorCulture("ja-JP");

        expect(getBlazorCulture()).toBe("ja-JP");
        expect(storedValues.get("theme")).toBe("dark");
        expect(storedValues.size).toBe(2);
    });

    test("reports a storage read error to the caller", () => {
        const error = new DOMException("Storage is blocked", "SecurityError");
        browserWindow.localStorage.getItem = () => { throw error; };

        expect(() => getBlazorCulture()).toThrow(error);
    });

    test("reports a storage write error to the caller", () => {
        const error = new DOMException("Storage is full", "QuotaExceededError");
        browserWindow.localStorage.setItem = () => { throw error; };

        expect(() => setBlazorCulture("fr-FR")).toThrow(error);
    });
});

describe("navigator languages", () => {
    test("resolves the browser's primary language", async () => {
        browserNavigator.language = "et-EE";

        expect(navigatorLanguage()).toBeInstanceOf(Promise);
        expect(await navigatorLanguage()).toBe("et-EE");
    });

    test("preserves language preference order", async () => {
        browserNavigator.languages = Object.freeze(["en-US", "ru-RU", "ja-JP"]);

        expect(await navigatorLanguages()).toEqual(["en-US", "ru-RU", "ja-JP"]);
    });

    test("returns an empty list when the browser has no language preferences", async () => {
        browserNavigator.languages = [];

        expect(await navigatorLanguages()).toEqual([]);
    });

    test("reads changed browser preferences on each call", async () => {
        expect(await navigatorLanguage()).toBe("en-US");
        expect(await navigatorLanguages()).toEqual(["en-US", "ja-JP"]);
        browserNavigator.language = "fr-FR";
        browserNavigator.languages = ["fr-FR"];

        expect(await navigatorLanguage()).toBe("fr-FR");
        expect(await navigatorLanguages()).toEqual(["fr-FR"]);
    });
});

describe("query culture", () => {
    test.each([
        ["", "culture", null],
        ["?other=value", "culture", null],
        ["?culture=fr-FR&ui-culture=ja-JP", "culture", "fr-FR"],
        ["?culture=fr-FR&ui-culture=ja-JP", "ui-culture", "ja-JP"],
        ["?culture=", "culture", ""],
        ["?culture", "culture", ""],
        ["?culture=fr-FR&culture=ja-JP", "culture", "fr-FR"],
        ["?Culture=fr-FR", "culture", null],
        ["?language=et-EE", "language", "et-EE"],
        ["?culture=fr%2DFR", "culture", "fr-FR"],
        ["?culture=hello+world", "culture", "hello world"],
        ["?culture=fr%26other%3Dvalue", "culture", "fr&other=value"],
        ["?c%2Bulture=ja-JP", "c+ulture", "ja-JP"],
        ["?culture=%E6%97%A5%E6%9C%AC%E8%AA%9E", "culture", "\u65e5\u672c\u8a9e"]
    ])('reads "%s" using key "%s"', (search, key, expected) => {
        browserWindow.location.search = search;

        expect(getQueryValue(key)).toBe(expected);
    });

    test("reads the current URL after navigation", () => {
        browserWindow.location.search = "?culture=en-US";
        expect(getQueryValue("culture")).toBe("en-US");
        browserWindow.location.search = "?culture=ja-JP";

        expect(getQueryValue("culture")).toBe("ja-JP");
    });
});
