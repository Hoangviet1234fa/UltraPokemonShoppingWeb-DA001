// Mouse Glow Effect
const mouseGlow = document.getElementById('mouseGlow');

if (mouseGlow) {
    document.addEventListener('mousemove', (e) => {
        mouseGlow.style.left = e.clientX + 'px';
        mouseGlow.style.top = e.clientY + 'px';
    });

    // Hide on mobile
    if (window.innerWidth <= 768) {
        mouseGlow.style.display = 'none';
    }

    window.addEventListener('resize', () => {
        if (window.innerWidth <= 768) {
            mouseGlow.style.display = 'none';
        } else {
            mouseGlow.style.display = 'block';
        }
    });
}

// Navbar transitions
const navbar = document.querySelector('.up-navbar');
let lastScrollTop = 0;

window.addEventListener('scroll', () => {
    const scrollTop = window.scrollY;

    if (scrollTop > 50) {
        navbar.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.2)';
    } else {
        navbar.style.boxShadow = '0 2px 12px rgba(0, 0, 0, 0.15)';
    }

    lastScrollTop = scrollTop;
});

// Smooth scroll for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        const href = this.getAttribute('href');
        if (href !== '#' && document.querySelector(href)) {
            e.preventDefault();
            const target = document.querySelector(href);
            const navHeight = document.querySelector('.up-header').offsetHeight;
            const targetPosition = target.offsetTop - navHeight;

            window.scrollTo({
                top: targetPosition,
                behavior: 'smooth'
            });
        }
    });
});

// Search box placeholder animation
const searchInput = document.querySelector('.up-search-input');
if (searchInput) {
    searchInput.addEventListener('focus', function() {
        this.parentElement.classList.add('focused');
    });

    searchInput.addEventListener('blur', function() {
        this.parentElement.classList.remove('focused');
    });
}

// Cart count animation
const cartCount = document.querySelector('.up-cart-count');
if (cartCount && cartCount.textContent !== '0') {
    cartCount.addEventListener('click', function(e) {
        e.preventDefault();
        this.style.animation = 'none';
        setTimeout(() => {
            this.style.animation = 'bounce 0.6s cubic-bezier(0.22, 1, 0.36, 1)';
        }, 10);
    });
}

// Add bounce animation
const style = document.createElement('style');
style.textContent = `
    @keyframes bounce {
        0%, 100% {
            transform: scale(1);
        }
        50% {
            transform: scale(1.2);
        }
    }
`;
document.head.appendChild(style);
