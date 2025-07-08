document.addEventListener("DOMContentLoaded", function () {
    // Tạo nút chuyển đổi theme
    const switcher = document.createElement("button");
    switcher.innerText = "Switch to Dark Theme";
    switcher.style.position = "fixed"; // Có thể tùy chỉnh
    switcher.style.top = "10px";
    switcher.style.right = "10px";
    switcher.style.zIndex = "1000";
    switcher.style.padding = "10px";
    switcher.style.backgroundColor = "#007bff";
    switcher.style.color = "#fff";
    switcher.style.border = "none";
    switcher.style.borderRadius = "5px";
    switcher.style.cursor = "pointer";

    // Thêm nút vào trang
    document.body.appendChild(switcher);

    // Trạng thái theme
    let isDarkTheme = false;

    // Chuyển đổi theme khi nhấn nút
    switcher.addEventListener("click", function () {
        const themeStyle = document.getElementById("dark-theme-style");

        if (!isDarkTheme) {
            // Áp dụng dark theme
            if (!themeStyle) {
                const style = document.createElement("link");
                style.id = "dark-theme-style";
                style.rel = "stylesheet";
                style.href = "/swagger-dark-theme.css"; // Đường dẫn tới dark-theme.css
                document.head.appendChild(style);
            }
            switcher.innerText = "Switch to Default Theme";
            isDarkTheme = true;
        } else {
            // Gỡ dark theme, quay lại mặc định
            if (themeStyle) {
                themeStyle.remove();
            }
            switcher.innerText = "Switch to Dark Theme";
            isDarkTheme = false;
        }
    });
});