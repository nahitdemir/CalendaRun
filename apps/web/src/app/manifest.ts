import type { MetadataRoute } from "next";
import { BRAND } from "@/constants/brand";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: BRAND.name,
    short_name: BRAND.name,
    start_url: "/",
    display: "standalone",
    background_color: "#ffffff",
    theme_color: "#ffffff",
    icons: [
      {
        src: BRAND.assets.icon192,
        sizes: "192x192",
        type: "image/png",
      },
      {
        src: BRAND.assets.icon512,
        sizes: "512x512",
        type: "image/png",
      },
    ],
  };
}
