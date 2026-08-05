document.addEventListener("DOMContentLoaded", () => {
    initialiseMobileMenu();
    initialiseScrollAnimations();
});

function initialiseMobileMenu() {
    const menuToggle = document.querySelector(".menu-toggle");
    const menu = document.querySelector(".menu");

    if (!menuToggle || !menu) {
        return;
    }

    menuToggle.addEventListener("click", () => {
        const isOpen = menu.classList.toggle("open");

        menuToggle.setAttribute(
            "aria-expanded",
            String(isOpen)
        );
    });

    menu.querySelectorAll("a").forEach((link) => {
        link.addEventListener("click", () => {
            menu.classList.remove("open");
            menuToggle.setAttribute("aria-expanded", "false");
        });
    });
}

function initialiseScrollAnimations() {
    const elements = document.querySelectorAll(".reveal");

    if (elements.length === 0) {
        return;
    }

    if (!("IntersectionObserver" in window)) {
        elements.forEach((element) => {
            element.classList.add("revealed");
        });

        return;
    }

    const observer = new IntersectionObserver(
        (entries, currentObserver) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) {
                    return;
                }

                entry.target.classList.add("revealed");
                currentObserver.unobserve(entry.target);
            });
        },
        {
            threshold: 0.15
        }
    );

    elements.forEach((element) => {
        observer.observe(element);
    });
}