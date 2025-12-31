import fs from "fs/promises";
import path from "path";
import { fileURLToPath } from "url";
import { createRequire } from "module";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const webDir = path.resolve(__dirname, "..");
const publicDir = path.join(webDir, "public");
const brandDir = path.join(publicDir, "brand", "calendarun");
const iconsDir = path.join(brandDir, "icons");
const appDir = path.join(webDir, "src", "app");
const sourceImage = path.join(webDir, "assets", "brand", "calendarun-logo-sheet.png");

const require = createRequire(path.join(webDir, "package.json"));
const sharp = require("sharp");

const CONFIG = {
  background: {
    colors: [
      { r: 255, g: 255, b: 255 },
      { r: 254, g: 254, b: 254 },
      { r: 253, g: 253, b: 253 },
      { r: 222, g: 222, b: 222 },
      { r: 221, g: 221, b: 221 },
      { r: 220, g: 220, b: 220 },
      { r: 224, g: 224, b: 224 },
      { r: 223, g: 223, b: 223 },
      { r: 219, g: 219, b: 219 },
    ],
    bgThreshold: 20,
    fgThreshold: 120,
    snapLow: 0.05,
    snapHigh: 0.95,
  },
  wordmark: {
    crop: { left: 91, top: 110, width: 756, height: 182 },
    output: path.join(brandDir, "wordmark.png"),
  },
  icons: [
    {
      name: "icon-16.png",
      size: 16,
      crop: { left: 83, top: 616, width: 44, height: 46 },
      output: path.join(iconsDir, "icon-16.png"),
    },
    {
      name: "icon-32.png",
      size: 32,
      crop: { left: 174, top: 604, width: 54, height: 58 },
      output: path.join(iconsDir, "icon-32.png"),
    },
    {
      name: "icon-48.png",
      size: 48,
      crop: { left: 274, top: 584, width: 73, height: 78 },
      output: path.join(iconsDir, "icon-48.png"),
    },
    {
      name: "icon-64.png",
      size: 64,
      crop: { left: 393, top: 562, width: 95, height: 100 },
      output: path.join(iconsDir, "icon-64.png"),
    },
    {
      name: "icon-128.png",
      size: 128,
      crop: { left: 530, top: 524, width: 129, height: 134 },
      output: path.join(iconsDir, "icon-128.png"),
    },
    {
      name: "apple-180.png",
      size: 180,
      crop: { left: 701, top: 499, width: 152, height: 159 },
      output: path.join(iconsDir, "apple-180.png"),
    },
    {
      name: "icon-192.png",
      size: 192,
      crop: { left: 897, top: 482, width: 173, height: 180 },
      output: path.join(iconsDir, "icon-192.png"),
    },
    {
      name: "icon-512.png",
      size: 512,
      crop: { left: 1117, top: 452, width: 214, height: 217 },
      output: path.join(iconsDir, "icon-512.png"),
    },
  ],
  appIcons: {
    icon: {
      source: "icon-32.png",
      output: path.join(appDir, "icon.png"),
    },
    apple: {
      source: "apple-180.png",
      output: path.join(appDir, "apple-icon.png"),
    },
  },
};

const ensureDir = (dir) => fs.mkdir(dir, { recursive: true });

const distance = (a, b) => {
  const dr = a.r - b.r;
  const dg = a.g - b.g;
  const db = a.b - b.b;
  return Math.sqrt(dr * dr + dg * dg + db * db);
};

const unblendCrop = async (crop, background) => {
  const { data, info } = await sharp(sourceImage)
    .extract(crop)
    .raw()
    .toBuffer({ resolveWithObject: true });
  const { width, height, channels } = info;
  const out = Buffer.alloc(width * height * 4);

  for (let i = 0; i < width * height; i++) {
    const pixel = {
      r: data[i * channels],
      g: data[i * channels + 1],
      b: data[i * channels + 2],
    };

    let bestBg = background.colors[0];
    let minDist = distance(pixel, bestBg);
    for (const bg of background.colors.slice(1)) {
      const d = distance(pixel, bg);
      if (d < minDist) {
        minDist = d;
        bestBg = bg;
      }
    }

    let alpha;
    if (minDist <= background.bgThreshold) {
      alpha = 0;
    } else if (minDist >= background.fgThreshold) {
      alpha = 1;
    } else {
      alpha =
        (minDist - background.bgThreshold) /
        (background.fgThreshold - background.bgThreshold);
    }

    if (alpha < background.snapLow) alpha = 0;
    if (alpha > background.snapHigh) alpha = 1;

    let r = 0;
    let g = 0;
    let b = 0;
    if (alpha > 0) {
      r = (pixel.r - (1 - alpha) * bestBg.r) / alpha;
      g = (pixel.g - (1 - alpha) * bestBg.g) / alpha;
      b = (pixel.b - (1 - alpha) * bestBg.b) / alpha;
    }

    out[i * 4] = Math.max(0, Math.min(255, Math.round(r)));
    out[i * 4 + 1] = Math.max(0, Math.min(255, Math.round(g)));
    out[i * 4 + 2] = Math.max(0, Math.min(255, Math.round(b)));
    out[i * 4 + 3] = Math.round(alpha * 255);
  }

  return { buffer: out, width, height };
};

const writeAsset = async ({ crop, size, output }) => {
  const { buffer, width, height } = await unblendCrop(crop, CONFIG.background);
  await ensureDir(path.dirname(output));

  let image = sharp(buffer, { raw: { width, height, channels: 4 } });
  if (size) {
    image = image.resize(size, size);
  }

  await image.png().toFile(output);
};

const main = async () => {
  await ensureDir(iconsDir);
  await ensureDir(appDir);

  try {
    await fs.access(sourceImage);
  } catch {
    throw new Error(`Missing source image at ${sourceImage}`);
  }

  await writeAsset(CONFIG.wordmark);

  for (const icon of CONFIG.icons) {
    await writeAsset(icon);
  }

  const iconSource = path.join(iconsDir, CONFIG.appIcons.icon.source);
  const appleSource = path.join(iconsDir, CONFIG.appIcons.apple.source);
  await fs.copyFile(iconSource, CONFIG.appIcons.icon.output);
  await fs.copyFile(appleSource, CONFIG.appIcons.apple.output);

  console.log("Brand assets generated from", sourceImage);
};

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
