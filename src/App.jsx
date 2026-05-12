import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Search, User, ShoppingBag, Menu, X, ArrowRight } from 'lucide-react';

const fadeUp = {
  hidden: { opacity: 0, y: 30 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.8, ease: "easeOut" } }
};

const staggerContainer = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.2 }
  }
};

const Navbar = () => {
  const [scrolled, setScrolled] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 50);
    };
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  return (
    <nav className={`fixed w-full z-50 transition-all duration-500 border-b border-transparent ${scrolled ? 'bg-navy/90 backdrop-blur-md border-gold/10 py-4 shadow-2xl' : 'bg-transparent py-6'}`}>
      <div className="max-w-[1400px] mx-auto px-6 md:px-12 flex justify-between items-center">
        {/* Mobile menu button */}
        <button className="md:hidden text-cream" onClick={() => setMobileMenuOpen(true)}>
          <Menu size={24} />
        </button>

        <a href="#" className="font-serif text-2xl tracking-widest text-gold z-50 relative">VISSTA</a>
        
        <div className="hidden md:flex space-x-10 text-xs tracking-[0.2em]">
          <a href="#" className="hover:text-gold transition-colors duration-300">NEW ARRIVALS</a>
          <a href="#" className="hover:text-gold transition-colors duration-300">POLOS</a>
          <a href="#" className="hover:text-gold transition-colors duration-300">TEES</a>
          <a href="#" className="hover:text-gold transition-colors duration-300">COLLECTIONS</a>
          <a href="#" className="hover:text-gold transition-colors duration-300">ABOUT</a>
        </div>

        <div className="flex space-x-6 z-50 relative">
          <button className="hover:text-gold transition-colors duration-300"><Search size={20} className="font-extralight"/></button>
          <button className="hover:text-gold transition-colors duration-300 hidden md:block"><User size={20} className="font-extralight"/></button>
          <button className="hover:text-gold transition-colors duration-300"><ShoppingBag size={20} className="font-extralight"/></button>
        </div>
      </div>

      {/* Mobile Menu */}
      <AnimatePresence>
        {mobileMenuOpen && (
          <motion.div 
            initial={{ opacity: 0, x: '-100%' }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: '-100%' }}
            transition={{ type: 'tween', duration: 0.4 }}
            className="fixed inset-0 bg-navy z-40 flex flex-col justify-center items-center h-screen w-screen p-8"
          >
            <button className="absolute top-6 left-6 md:left-12 text-cream" onClick={() => setMobileMenuOpen(false)}>
              <X size={32} />
            </button>
            <div className="flex flex-col space-y-8 text-center text-sm tracking-[0.2em]">
              <a href="#" className="hover:text-gold transition-colors duration-300">NEW ARRIVALS</a>
              <a href="#" className="hover:text-gold transition-colors duration-300">POLOS</a>
              <a href="#" className="hover:text-gold transition-colors duration-300">TEES</a>
              <a href="#" className="hover:text-gold transition-colors duration-300">COLLECTIONS</a>
              <a href="#" className="hover:text-gold transition-colors duration-300">ABOUT</a>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </nav>
  );
};

const Hero = () => {
  return (
    <section className="relative min-h-[90vh] flex items-center bg-navy pt-20 overflow-hidden">
      {/* Full height absolute image on the right */}
      <div className="absolute top-0 right-0 w-full md:w-3/5 lg:w-[55%] h-full z-0">
        {/* Navy gradient shadow from left to blend with background */}
        <div className="absolute inset-0 bg-gradient-to-r from-navy via-navy/60 to-transparent z-10 pointer-events-none w-[120%] -ml-[20%]"></div>
        <div className="absolute inset-0 bg-gradient-to-t from-navy via-transparent to-transparent z-10 pointer-events-none h-full"></div>
        
        <motion.img 
          initial={{ opacity: 0, scale: 1.05 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 1.5, ease: "easeOut" }}
          src="./assets/hero-model.webp" 
          alt="Male model in luxury clothing" 
          className="w-full h-full object-cover object-center opacity-90"
        />
      </div>

      <div className="max-w-[1400px] mx-auto px-6 md:px-12 w-full relative z-10">
        <div className="w-full md:w-1/2 min-h-[60vh] flex flex-col justify-center">
          <motion.div 
            initial="hidden"
            animate="visible"
            variants={staggerContainer}
          >
            <motion.p variants={fadeUp} className="text-gold tracking-[0.2em] text-xs font-semibold mb-6 uppercase">
              Timeless. Refined. Effortless.
            </motion.p>
            <motion.h1 variants={fadeUp} className="font-serif text-5xl md:text-7xl lg:text-8xl leading-tight mb-8 text-cream">
              The art of <br /> quiet luxury.
            </motion.h1>
            <motion.p variants={fadeUp} className="text-lg md:text-xl text-cream/80 max-w-md font-light mb-12">
              Elevated essentials, crafted for a life well-dressed.
            </motion.p>
            <motion.button variants={fadeUp} className="bg-gold text-navy px-8 py-4 text-xs tracking-widest uppercase font-medium hover:bg-cream hover:text-navy transition-all duration-300 rounded-sm">
              Shop Collection
            </motion.button>
          </motion.div>
        </div>
      </div>
    </section>
  );
};

const brandPhilosophy = () => {
  return (
    <section className="py-24 bg-gradient-to-b from-navy-light to-navy relative">
      <div className="absolute top-0 w-full h-px bg-gradient-to-r from-transparent via-gold/20 to-transparent"></div>
      <motion.div 
        initial="hidden"
        whileInView="visible"
        viewport={{ once: true, margin: "-100px" }}
        variants={staggerContainer}
        className="max-w-4xl mx-auto px-6 text-center"
      >
        <motion.h3 variants={fadeUp} className="text-gold tracking-[0.3em] text-xs md:text-sm mb-6 uppercase">
          Old Money. Modern Mindset.
        </motion.h3>
        <motion.p variants={fadeUp} className="text-xl md:text-3xl font-serif text-cream leading-relaxed">
          Clean silhouettes. Premium fabrics. Understated details. <br className="hidden md:block"/> Pieces that speak without saying a word.
        </motion.p>
      </motion.div>
    </section>
  );
};

const FeaturedGrid = () => {
  return (
    <section className="py-12 bg-navy">
      <div className="max-w-[1400px] mx-auto px-6 md:px-12">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <motion.div 
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.8 }}
            className="group overflow-hidden rounded-sm relative aspect-[4/5] bg-navy-dark border border-white/5"
          >
            <img src="./assets/knit-grey.webp" alt="Grey knit polo" className="w-full h-full object-cover transition-transform duration-1000 group-hover:scale-105 opacity-80 group-hover:opacity-100" />
            <div className="absolute inset-0 bg-black/20 group-hover:bg-transparent transition-colors duration-700"></div>
          </motion.div>
          <motion.div 
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.8, delay: 0.2 }}
            className="group overflow-hidden rounded-sm relative aspect-[4/5] bg-navy-dark border border-white/5"
          >
            <img src="./assets/folded-clothes.webp" alt="Folded premium clothes" className="w-full h-full object-cover transition-transform duration-1000 group-hover:scale-105 opacity-80 group-hover:opacity-100" />
            <div className="absolute inset-0 bg-black/20 group-hover:bg-transparent transition-colors duration-700"></div>
          </motion.div>
          <motion.div 
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.8, delay: 0.4 }}
            className="group overflow-hidden rounded-sm relative aspect-[4/5] bg-navy-dark border border-white/5"
          >
            <img src="./assets/knit-white.webp" alt="White knit polo" className="w-full h-full object-cover transition-transform duration-1000 group-hover:scale-105 opacity-80 group-hover:opacity-100" />
            <div className="absolute inset-0 bg-black/20 group-hover:bg-transparent transition-colors duration-700"></div>
          </motion.div>
        </div>
      </div>
    </section>
  );
};

const FeatureIconStrip = () => {
  const features = [
    { title: "PREMIUM FABRICS", desc: "Carefully selected for comfort and quality.", icon: (
      <svg className="w-8 h-8 mb-4 stroke-gold fill-transparent stroke-[1.5]" viewBox="0 0 24 24" fill="none" stroke="currentColor" xmlns="http://www.w3.org/2000/svg">
        <circle cx="12" cy="12" r="10"></circle>
        <path d="M12 2a14.5 14.5 0 0 0 0 20"></path>
        <path d="M2 12h20"></path>
        <path d="M12 2a14.5 14.5 0 0 1 0 20"></path>
      </svg>
    )},
    { title: "TIMELESS DESIGNS", desc: "Classic pieces that never go out of style.", icon: (
      <svg className="w-8 h-8 mb-4 stroke-gold fill-transparent stroke-[1.5]" viewBox="0 0 24 24" fill="none" stroke="currentColor" xmlns="http://www.w3.org/2000/svg">
        <circle cx="12" cy="12" r="10"></circle>
        <polyline points="12 6 12 12 16 14"></polyline>
      </svg>
    )},
    { title: "EFFORTLESS STYLE", desc: "Versatile staples for every occasion.", icon: (
      <svg className="w-8 h-8 mb-4 stroke-gold fill-transparent stroke-[1.5]" viewBox="0 0 24 24" fill="none" stroke="currentColor" xmlns="http://www.w3.org/2000/svg">
        <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"></path>
        <line x1="4" y1="22" x2="4" y2="15"></line>
      </svg>
    )},
    { title: "BUILT TO LAST", desc: "Durable craftsmanship you can rely on.", icon: (
      <svg className="w-8 h-8 mb-4 stroke-gold fill-transparent stroke-[1.5]" viewBox="0 0 24 24" fill="none" stroke="currentColor" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
        <polyline points="9 12 11 14 15 10"></polyline>
      </svg>
    )}
  ];

  return (
    <section className="py-20 bg-navy relative border-y border-white/5">
      <div className="max-w-[1400px] mx-auto px-6 md:px-12 relative z-10">
        <motion.div 
          initial="hidden"
          whileInView="visible"
          viewport={{ once: true }}
          variants={staggerContainer}
          className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4"
        >
          {features.map((feature, i) => (
            <motion.div variants={fadeUp} key={i} className="glassmorphism p-8 rounded-sm text-center md:text-left flex flex-col items-center md:items-start group hover:bg-navy-light/60 transition-colors duration-500">
              {feature.icon}
              <h4 className="text-gold tracking-[0.2em] text-xs font-semibold mb-3 mt-4">{feature.title}</h4>
              <p className="text-cream/70 text-sm font-light leading-relaxed">{feature.desc}</p>
            </motion.div>
          ))}
        </motion.div>
      </div>
    </section>
  );
};

const NewCollection = () => {
  const products = [
    { name: "Textured Knit Polo", price: "750 EGP", img: "./assets/product-white-polo.webp" },
    { name: "Ribbed Knit Polo", price: "700 EGP", img: "./assets/product-grey-polo.webp" },
    { name: "Cable Knit Polo", price: "700 EGP", img: "./assets/product-knit-polo.webp" },
    { name: "Contrast Knit Tee", price: "650 EGP", img: "./assets/product-black-tee.webp" }
  ];

  return (
    <section className="py-24 bg-gradient-to-t from-navy to-navy-light">
      <div className="max-w-[1400px] mx-auto px-6 md:px-12">
        <div className="flex flex-col xl:flex-row gap-16">
          <motion.div 
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true }}
            variants={staggerContainer}
            className="xl:w-1/3 flex flex-col justify-center"
          >
            <motion.p variants={fadeUp} className="text-gold tracking-[0.2em] text-xs font-semibold mb-6 uppercase">NEW COLLECTION</motion.p>
            <motion.h2 variants={fadeUp} className="font-serif text-5xl md:text-6xl text-cream mb-6">Summer <br/>Essentials</motion.h2>
            <motion.p variants={fadeUp} className="text-cream/80 text-lg font-light mb-10 max-w-sm">
              Lightweight knits and breathable fabrics for the season ahead.
            </motion.p>
            <motion.div variants={fadeUp}>
              <button className="bg-gold text-navy px-8 py-4 text-xs tracking-widest uppercase font-medium hover:bg-cream hover:text-navy transition-all duration-300 rounded-sm">
                Explore Now
              </button>
            </motion.div>
          </motion.div>

          <div className="xl:w-2/3">
            <motion.div 
              initial="hidden"
              whileInView="visible"
              viewport={{ once: true }}
              variants={staggerContainer}
              className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6"
            >
              {products.map((product, i) => (
                <motion.div variants={fadeUp} key={i} className="group cursor-pointer">
                  <div className="aspect-[3/4] overflow-hidden rounded-sm bg-navy-dark border border-white/5 mb-6 relative">
                    <img src={product.img} alt={product.name} className="w-full h-full object-cover transition-transform duration-1000 group-hover:scale-105 opacity-90 group-hover:opacity-100" />
                    <div className="absolute inset-x-0 bottom-0 p-4 bg-gradient-to-t from-black/60 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex justify-center">
                      <span className="text-xs tracking-widest text-cream uppercase border-b border-cream pb-1">Quick Add</span>
                    </div>
                  </div>
                  <h4 className="text-sm font-medium tracking-wide text-cream mb-2 uppercase">{product.name}</h4>
                  <p className="text-gold text-sm">{product.price}</p>
                </motion.div>
              ))}
            </motion.div>
          </div>
        </div>
      </div>
    </section>
  );
};

const EditorialPromise = () => {
  return (
    <section className="bg-navy relative border-y border-white/5">
      <div className="grid grid-cols-1 lg:grid-cols-2">
        <motion.div 
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          transition={{ duration: 1 }}
          className="h-[50vh] lg:h-[80vh] bg-navy-dark relative"
        >
          <img src="./assets/premium-folded.webp" alt="Premium Folded Clothes" className="w-full h-full object-cover opacity-80" />
        </motion.div>
        
        <div className="flex items-center justify-center p-12 lg:p-24 relative overflow-hidden">
          {/* Spotlight gradient effect */}
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[150%] h-[150%] bg-radial-gradient from-gold/5 via-transparent to-transparent opacity-50 pointer-events-none"></div>
          
          <motion.div 
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true }}
            variants={staggerContainer}
            className="max-w-md relative z-10"
          >
            <motion.p variants={fadeUp} className="text-gold tracking-[0.2em] text-xs font-semibold mb-6 uppercase">
              Our Promise
            </motion.p>
            <motion.h2 variants={fadeUp} className="font-serif text-4xl md:text-5xl text-cream mb-6 leading-tight">
              Quality Over Everything
            </motion.h2>
            <motion.p variants={fadeUp} className="text-cream/70 text-lg font-light mb-10 leading-relaxed">
              We believe in fewer, better pieces. Thoughtfully made with integrity in every stitch.
            </motion.p>
            <motion.a variants={fadeUp} href="#" className="inline-flex items-center text-xs tracking-widest text-cream uppercase border-b border-gold/40 hover:border-gold pb-1 transition-colors group">
              Learn More <ArrowRight size={14} className="ml-2 group-hover:translate-x-1 transition-transform"/>
            </motion.a>
          </motion.div>
        </div>
      </div>
    </section>
  );
};

const Newsletter = () => {
  return (
    <section className="py-32 bg-navy relative overflow-hidden text-center">
      <div className="absolute top-0 w-full h-px bg-gradient-to-r from-transparent via-gold/10 to-transparent"></div>
      
      <motion.div 
        initial="hidden"
        whileInView="visible"
        viewport={{ once: true }}
        variants={staggerContainer}
        className="max-w-2xl mx-auto px-6 relative z-10"
      >
        <motion.h2 variants={fadeUp} className="font-serif text-4xl md:text-5xl text-cream mb-4">
          Join the Club
        </motion.h2>
        <motion.p variants={fadeUp} className="text-cream/70 text-lg font-light mb-12">
          Early access. New drops. Exclusive offers.
        </motion.p>
        
        <motion.form variants={fadeUp} className="flex flex-col sm:flex-row gap-4 justify-center max-w-md mx-auto relative group">
          <input 
            type="email" 
            placeholder="Enter your email" 
            className="w-full bg-navy-light/50 border border-white/10 rounded-sm px-6 py-4 text-sm text-cream placeholder-cream/40 focus:outline-none focus:border-gold/50 transition-colors backdrop-blur-sm"
          />
          <button type="submit" className="bg-gold text-navy px-8 py-4 text-xs tracking-widest uppercase font-medium hover:bg-cream hover:text-navy transition-all duration-300 rounded-sm whitespace-nowrap">
            Subscribe
          </button>
        </motion.form>
      </motion.div>
    </section>
  );
};

const Footer = () => {
  return (
    <footer className="bg-navy-dark pt-20 pb-10 border-t border-white/5">
      <div className="max-w-[1400px] mx-auto px-6 md:px-12">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-10 mb-20">
          <div>
            <h4 className="text-gold text-xs tracking-[0.2em] font-medium uppercase mb-6">Shop</h4>
            <ul className="space-y-4 text-sm text-cream/60 font-light">
              <li><a href="#" className="hover:text-cream transition-colors">New Arrivals</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Polos & Knits</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">T-Shirts</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Shirts</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Pants</a></li>
            </ul>
          </div>
          <div>
            <h4 className="text-gold text-xs tracking-[0.2em] font-medium uppercase mb-6">Company</h4>
            <ul className="space-y-4 text-sm text-cream/60 font-light">
              <li><a href="#" className="hover:text-cream transition-colors">About</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Sustainability</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Stores</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Careers</a></li>
            </ul>
          </div>
          <div>
            <h4 className="text-gold text-xs tracking-[0.2em] font-medium uppercase mb-6">Support</h4>
            <ul className="space-y-4 text-sm text-cream/60 font-light">
              <li><a href="#" className="hover:text-cream transition-colors">FAQ</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Shipping</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Returns</a></li>
              <li><a href="#" className="hover:text-cream transition-colors">Contact</a></li>
            </ul>
          </div>
          <div>
            <h4 className="text-gold text-xs tracking-[0.2em] font-medium uppercase mb-6">Socials</h4>
            <ul className="space-y-4 text-sm text-cream/60 font-light">
              <li><a href="https://www.instagram.com/vissta.eg/" target="_blank" rel="noreferrer" className="hover:text-cream transition-colors">Instagram</a></li>
              <li><a href="https://www.facebook.com/profile.php?id=61576275821044" target="_blank" rel="noreferrer" className="hover:text-cream transition-colors">Facebook</a></li>
            </ul>
          </div>
        </div>
        
        <div className="pt-8 border-t border-white/10 flex flex-col md:flex-row justify-between items-center text-xs text-cream/40 font-light">
          <p>© 2026 VISSTA. All rights reserved.</p>
          <div className="flex space-x-6 mt-4 md:mt-0">
            <a href="#" className="hover:text-cream transition-colors">Privacy Policy</a>
            <a href="#" className="hover:text-cream transition-colors">Terms of Service</a>
          </div>
        </div>
      </div>
    </footer>
  );
};

function App() {
  return (
    <div className="min-h-screen bg-navy selection:bg-gold/30 selection:text-cream font-sans">
      <Navbar />
      <Hero />
      {brandPhilosophy()}
      <FeaturedGrid />
      <FeatureIconStrip />
      <NewCollection />
      <EditorialPromise />
      <Newsletter />
      <Footer />
    </div>
  )
}

export default App
