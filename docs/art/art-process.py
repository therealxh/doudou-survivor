from PIL import Image
import os

# 美术素材处理流水线（换图时复用）：
# 1) 裁掉底部水印区  2) 近白/浅灰抠透明（保留暖色与彩色）  3) 裁到内容边界  4) 缩放到最长边 256
# 用法：把生成的贴图放 src_dir，在 jobs 里登记（文件名, 输出名），运行本脚本

src_dir = r"C:\Users\31997\.qoder-cn\vibe_images"
out_dir = r"f:\秋招小游戏项目\豆豆幸存者\Assets\Resources\Sprites"
os.makedirs(out_dir, exist_ok=True)

jobs = [
    ("player-archer-topdown_1789454241.png", "player"),
    ("enemy-monster-topdown_1789454241.png", "enemy"),
    ("arrow-projectile-topdown_1789454242.png", "arrow"),
    ("bomb-grenade-topdown_1789457890.png", "bomb"),
    ("explosion-blast-topdown_1789458634.png", "explosion"),
    ("throwing-dart-topdown_1789460026.png", "dart"),
    ("boomerang-topdown_1789460024.png", "boomerang"),
]

for fname, out in jobs:
    img = Image.open(os.path.join(src_dir, fname)).convert("RGBA")
    w, h = img.size
    img = img.crop((0, 0, w, h - 130))  # 裁水印
    px = img.load()
    W, H = img.size
    for y in range(H):
        for x in range(W):
            r, g, b, a = px[x, y]
            # 近白/浅灰（含软阴影）→ 透明；保留暖色（肤色等）与彩色内容
            if min(r, g, b) > 205 and (max(r, g, b) - min(r, g, b)) < 20:
                px[x, y] = (r, g, b, 0)
    bbox = img.getbbox()
    pad = 6
    x0 = max(0, bbox[0] - pad); y0 = max(0, bbox[1] - pad)
    x1 = min(W, bbox[2] + pad); y1 = min(H, bbox[3] + pad)
    img = img.crop((x0, y0, x1, y1))
    img.thumbnail((256, 256), Image.LANCZOS)
    dst = os.path.join(out_dir, out + ".png")
    img.save(dst)
    print(out, "->", img.size, dst)

print("ALL DONE")
