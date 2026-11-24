import { Nav } from "@/components/navigation/Nav";
import { Hero } from "@/components/hero/Hero";
import { NotWallpaper } from "@/components/sections/NotWallpaper";
import { Play } from "@/components/showcase/Play";
import { Details } from "@/components/sections/Details";
import { Visitors } from "@/components/sections/Visitors";
import { Lights } from "@/components/sections/Lights";
import { Platforms } from "@/components/sections/Platforms";
import { Install } from "@/components/installation/Install";
import { Source } from "@/components/sections/Source";
import { Faq } from "@/components/faq/Faq";
import { Finale } from "@/components/sections/Finale";
import { Footer } from "@/components/footer/Footer";

export default function Home() {
  return (
    <>
      <a href="#experience" className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-[60] focus:rounded focus:bg-snow focus:px-3 focus:py-2">
        Skip to content
      </a>
      <Nav />
      <main>
        <Hero />
        <NotWallpaper />
        <Play />
        <Details />
        <Visitors />
        <Lights />
        <Platforms />
        <Install />
        <Source />
        <Faq />
        <Finale />
      </main>
      <Footer />
    </>
  );
}
