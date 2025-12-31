import fs from "fs/promises";
import path from "path";
import { fileURLToPath } from "url";
import { createRequire } from "module";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const rootDir = path.resolve(__dirname, "..");
const webDir = path.join(rootDir, "apps", "web");
const publicDir = path.join(webDir, "public");
const brandDir = path.join(publicDir, "brand");
const appDir = path.join(webDir, "src", "app");
const sourceSvg = path.join(brandDir, "logo-mark.svg");

const require = createRequire(path.join(webDir, "package.json"));
const sharp = require("sharp");
const pngToIco = require("png-to-ico");

const ensureDir = (dir) => fs.mkdir(dir, { recursive: true });

const createPaddedPng = async (size) => {
  const padding = Math.max(2, Math.round(size * 0.12));
  const innerSize = size - padding * 2;
  const svgBuffer = await sharp(sourceSvg, { density: 600 })
    .resize(innerSize, innerSize, { fit: "contain" })
    .png()
    .toBuffer();

  return sharp({
    create: {
      width: size,
      height: size,
      channels: 4,
      background: { r: 0, g: 0, b: 0, alpha: 0 },
    },
  })
    .composite([{ input: svgBuffer, gravity: "center" }])
    .png()
    .toBuffer();
};

const writePng = async (size, outputPath, cache) => {
  if (!cache.has(size)) {
    cache.set(size, await createPaddedPng(size));
  }
  await ensureDir(path.dirname(outputPath));
  await fs.writeFile(outputPath, cache.get(size));
};

const main = async () => {
  await ensureDir(brandDir);
  await ensureDir(publicDir);
  await ensureDir(appDir);

  try {
    await fs.access(sourceSvg);
  } catch {
    throw new Error(`Missing source SVG at ${sourceSvg}`);
  }

  const cache = new Map();

  await writePng(512, path.join(appDir, "icon.png"), cache);
  await writePng(180, path.join(appDir, "apple-icon.png"), cache);
  await writePng(512, path.join(brandDir, "logo-mark.png"), cache);

  await writePng(16, path.join(publicDir, "favicon-16x16.png"), cache);
  await writePng(32, path.join(publicDir, "favicon-32x32.png"), cache);
  await writePng(192, path.join(publicDir, "android-chrome-192x192.png"), cache);
  await writePng(512, path.join(publicDir, "android-chrome-512x512.png"), cache);

  if (!cache.has(48)) {
    cache.set(48, await createPaddedPng(48));
  }

  const icoBuffer = await pngToIco([
    cache.get(16),
    cache.get(32),
    cache.get(48),
  ]);
  await fs.writeFile(path.join(publicDir, "favicon.ico"), icoBuffer);

  console.log("Icons generated from", sourceSvg);
};

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
