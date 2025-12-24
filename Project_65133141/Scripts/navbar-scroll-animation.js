// Scroll-based underline animation for red navbar
document.addEventListener('DOMContentLoaded', function () {
    const navLinks = document.querySelectorAll('#red-nav-menu .red-nav-link');
    const scrollUnderline = document.getElementById('nav-scroll-underline');
    const footer = document.querySelector('footer');

    if (!scrollUnderline || navLinks.length === 0) return;

    // Function to update underline position based on scroll
    function updateScrollUnderline() {
        const scrollPosition = window.scrollY + window.innerHeight / 2;
        const footerTop = footer ? footer.offsetTop : document.body.scrollHeight;

        // Hide underline when near footer
        if (window.scrollY + window.innerHeight >= footerTop - 100) {
            scrollUnderline.style.opacity = '0';
            return;
        } else {
            scrollUnderline.style.opacity = '1';
        }

        // Find active section
        let activeLink = navLinks[0]; // Default to first link

        navLinks.forEach(link => {
            const section = link.getAttribute('data-section');
            const targetElement = section === 'home'
                ? document.querySelector('#home, section:first-of-type')
                : document.querySelector(`#${section}`);

            if (targetElement) {
                const sectionTop = targetElement.offsetTop;
                const sectionBottom = sectionTop + targetElement.offsetHeight;

                if (scrollPosition >= sectionTop && scrollPosition < sectionBottom) {
                    activeLink = link;
                }
            }
        });

        // Update underline position
        const linkRect = activeLink.getBoundingClientRect();
        const navRect = activeLink.parentElement.getBoundingClientRect();

        scrollUnderline.style.left = (linkRect.left - navRect.left) + 'px';
        scrollUnderline.style.width = linkRect.width + 'px';

        // Update active class
        navLinks.forEach(link => link.classList.remove('active'));
        activeLink.classList.add('active');
    }

    // Initial update
    updateScrollUnderline();

    // Update on scroll
    window.addEventListener('scroll', updateScrollUnderline);
    window.addEventListener('resize', updateScrollUnderline);
});
