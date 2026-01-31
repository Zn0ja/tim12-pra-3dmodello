(function () {
    const KEY = "theme";

    function preferredTheme() {
        const saved = localStorage.getItem(KEY);
        if (saved === "light" || saved === "dark") return saved;

        return window.matchMedia &&
            window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    }

    function apply(theme) {
        document.body.classList.toggle("theme-dark", theme === "dark");

        const btn = document.getElementById("themeToggle");
        if (btn) btn.textContent = theme === "dark" ? "☀️" : "🌙";
    }

    window.toggleTheme = function () {
        const isDark = document.body.classList.contains("theme-dark");
        const next = isDark ? "light" : "dark";
        localStorage.setItem(KEY, next);
        apply(next);
    };

    document.addEventListener("DOMContentLoaded", function () {
        apply(preferredTheme());
    });
})();
