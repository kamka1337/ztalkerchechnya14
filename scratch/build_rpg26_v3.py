import numpy as np
from PIL import Image

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# Handcrafted Soviet Army Palette for RPG-26 "Аглень"
PAL = {
    # Tube Olive Drab (rich matte khaki green with cylindrical volume)
    'tube_spec':      hex_to_rgba('adcb66'), # Bright specular glint
    'tube_hi':        hex_to_rgba('8ea74f'), # Cylinder top highlight
    'tube_mid_hi':    hex_to_rgba('71883c'), # Upper body midtone
    'tube_mid':       hex_to_rgba('586b2e'), # Core olive paint
    'tube_shadow':    hex_to_rgba('3f4e1f'), # Lower body shadow
    'tube_dark':      hex_to_rgba('283313'), # Deep underside shade
    'tube_outline':   hex_to_rgba('141b09'), # Dark ambient outline

    # Black / Dark Steel / Matte Rubber
    'metal_spec':     hex_to_rgba('858d9c'), # Specular glint
    'metal_hi':       hex_to_rgba('636b77'), # Steel highlight
    'metal_mid_hi':   hex_to_rgba('474d57'), # Upper metal/rubber
    'metal_mid':      hex_to_rgba('2d323a'), # Core rubber/metal
    'metal_shadow':   hex_to_rgba('1c1f25'), # Dark rubber
    'metal_dark':     hex_to_rgba('121418'), # Deep shadow
    'metal_outline':  hex_to_rgba('0a0c0f'), # Deepest silhouette edge

    # Mechanical Steel Linkage Rod (тяга)
    'rod_hi':         hex_to_rgba('9ea89b'), # Steel highlight
    'rod_mid':        hex_to_rgba('6e776a'), # Steel rod body
    'rod_shadow':     hex_to_rgba('454c42'), # Rod shadow

    # Instruction Placard (табличка "НАПРАВЛЕНИЕ СТРЕЛЬБЫ")
    'placard_bg':     hex_to_rgba('141619'), # Deep charcoal
    'placard_edge':   hex_to_rgba('252a30'), # Placard border
    'arrow_white':    hex_to_rgba('ffffff'), # Bright white arrow
    'text_white':     hex_to_rgba('e2e8e3'), # White schematic markings
    'text_dim':       hex_to_rgba('88938a'), # Dim schematic markings

    # Red Cocking / Safety Latch
    'red_bright':     hex_to_rgba('ee372d'), # Safety red
    'red_dark':       hex_to_rgba('8e1a14'), # Shadow red

    # Sight Diopter Aperture
    'sight_glass':    hex_to_rgba('7598ae', 75), # Translucent aperture glass
}

canvas = np.zeros((48, 48, 4), dtype=np.uint8)

def p(x, y, color):
    if 0 <= x < 48 and 0 <= y < 48:
        canvas[y, x] = PAL[color]

def p_raw(x, y, color_rgba):
    if 0 <= x < 48 and 0 <= y < 48:
        canvas[y, x] = color_rgba

# ========================================================
# 1. MAIN LAUNCH TUBE BODY (Пусковая труба)
# ========================================================
# Center axis: t = 4 to 34
for t in range(4, 35):
    cx = 6 + t
    cy = 41 - t

    # Top outline
    p(cx - 1, cy - 2, 'tube_outline')
    p(cx - 2, cy - 1, 'tube_outline')

    # Cylinder highlight (light from top-left)
    p(cx - 1, cy - 1, 'tube_hi')

    # Upper midtone
    p(cx, cy - 1, 'tube_mid_hi')
    p(cx - 1, cy, 'tube_mid_hi')

    # Core base olive
    p(cx, cy, 'tube_mid')

    # Lower shadow
    p(cx + 1, cy, 'tube_shadow')
    p(cx, cy + 1, 'tube_shadow')

    # Deep underside shadow
    p(cx + 1, cy + 1, 'tube_dark')

    # Bottom outline
    p(cx + 2, cy + 1, 'tube_outline')
    p(cx + 1, cy + 2, 'tube_outline')

# Specular glint line along top
for t in range(6, 34, 2):
    cx = 6 + t; cy = 41 - t
    p(cx - 1, cy - 1, 'tube_spec')

# ========================================================
# 2. LINKAGE ROD (Тяга спускового механизма)
# ========================================================
# Steel rod running along the top from t=5 to t=21
for t in range(5, 22):
    cx = 6 + t; cy = 41 - t
    p(cx - 2, cy - 2, 'rod_hi')
    p(cx - 1, cy - 3, 'rod_shadow')
    p(cx - 3, cy - 1, 'rod_shadow')

# Rear mount bracket at t=5
cx = 6 + 5; cy = 41 - 5
p(cx - 2, cy - 2, 'metal_mid_hi')
p(cx - 3, cy - 2, 'metal_outline')
p(cx - 2, cy - 3, 'metal_outline')

# ========================================================
# 3. TRIGGER HOUSING & REAR SIGHT BASE (t = 21..24)
# ========================================================
for t in range(21, 25):
    cx = 6 + t; cy = 41 - t
    # Clamp collar around tube
    p(cx - 2, cy - 1, 'metal_outline')
    p(cx - 1, cy - 2, 'metal_outline')
    p(cx - 1, cy - 1, 'metal_mid_hi')
    p(cx, cy - 1, 'metal_mid')
    p(cx - 1, cy, 'metal_mid')
    p(cx + 2, cy + 1, 'metal_outline')
    p(cx + 1, cy + 2, 'metal_outline')
    p(cx + 2, cy + 2, 'metal_outline')

    # Trigger mechanism box
    p(cx - 2, cy - 2, 'metal_mid')
    p(cx - 3, cy - 2, 'metal_shadow')
    p(cx - 2, cy - 3, 'metal_shadow')
    p(cx - 3, cy - 3, 'metal_outline')

# Red safety / cocking indicator flag (at t=21)
cx = 6 + 21; cy = 41 - 21
p(cx - 2, cy - 2, 'red_bright')
p(cx - 1, cy - 2, 'red_dark')

# ========================================================
# 4. RAISED REAR SIGHT (Поднятая стойка прицела)
# ========================================================
# Erected upright from trigger block at t = 22 (cx = 28, cy = 19)
# Base hinge: (26, 17)
# Stalk:
p(25, 16, 'metal_outline')
p(26, 16, 'metal_mid_hi')
p(27, 16, 'metal_shadow')
p(28, 16, 'metal_outline')

p(25, 15, 'metal_outline')
p(26, 15, 'metal_hi')
p(27, 15, 'metal_mid')
p(28, 15, 'metal_outline')

p(24, 14, 'metal_outline')
p(25, 14, 'metal_hi')
p(26, 14, 'metal_mid')
p(27, 14, 'metal_outline')

# Diopter sight head with aperture peep hole:
p(24, 13, 'metal_outline')
p(25, 13, 'metal_spec') # Left blade
p(26, 13, 'metal_outline') # Peep aperture hole!
p(27, 13, 'metal_hi') # Right blade
p(28, 13, 'metal_outline')

# Top sighting arch and notch:
p(24, 12, 'metal_outline')
p(25, 12, 'metal_hi')
p(26, 12, 'metal_spec') # Center top notch
p(27, 12, 'metal_hi')
p(28, 12, 'metal_outline')

p(25, 11, 'metal_outline')
p(26, 11, 'metal_outline')

# ========================================================
# 5. INSTRUCTION PLACARD (Табличка с инструкцией)
# ========================================================
# Placed on the side of the tube from t = 14 to 21
for t in range(14, 21):
    cx = 6 + t; cy = 41 - t
    p(cx, cy - 1, 'placard_edge')
    p(cx - 1, cy, 'placard_edge')
    p(cx, cy, 'placard_bg')
    p(cx + 1, cy, 'placard_bg')
    p(cx, cy + 1, 'placard_bg')
    p(cx + 1, cy + 1, 'placard_edge')

# Directional arrow pointing forward to muzzle (top-right)
t = 20; cx = 6 + t; cy = 41 - t
p(cx, cy, 'arrow_white')
p(cx + 1, cy, 'arrow_white')

t = 19; cx = 6 + t; cy = 41 - t
p(cx, cy, 'arrow_white')
p(cx + 1, cy, 'placard_bg')

# Schematic markings
for t in [15, 16, 17, 18]:
    cx = 6 + t; cy = 41 - t
    p(cx, cy, 'text_white' if t % 2 == 0 else 'text_dim')
    p(cx + 1, cy, 'text_dim' if t % 2 == 0 else 'text_white')

# ========================================================
# 6. REAR END-CAP & EXHAUST BELL (Казённая часть / сопло)
# ========================================================
# Rear collar band (t = 3, 4)
for t in [3, 4]:
    cx = 6 + t; cy = 41 - t
    p(cx - 2, cy - 1, 'metal_mid_hi')
    p(cx - 1, cy - 2, 'metal_mid_hi')
    p(cx, cy - 1, 'metal_mid')
    p(cx - 1, cy, 'metal_mid')
    p(cx, cy, 'metal_shadow')
    p(cx + 1, cy, 'metal_shadow')
    p(cx, cy + 1, 'metal_shadow')
    p(cx + 1, cy + 1, 'metal_dark')
    p(cx + 2, cy + 1, 'metal_outline')
    p(cx + 1, cy + 2, 'metal_outline')

# Connect gap at (10, 36) and (9, 37) and (8, 37)
p(10, 36, 'metal_mid')
p(9, 37, 'metal_mid')
p(8, 37, 'metal_shadow')

# Rear black rubber cap (t = 1, 2)
for t in [1, 2]:
    cx = 6 + t; cy = 41 - t
    p(cx - 3, cy - 1, 'metal_outline')
    p(cx - 1, cy - 3, 'metal_outline')
    p(cx - 2, cy - 2, 'metal_hi')
    p(cx - 1, cy - 2, 'metal_mid')
    p(cx - 2, cy - 1, 'metal_mid')
    p(cx - 1, cy - 1, 'metal_mid')
    p(cx, cy - 1, 'metal_shadow')
    p(cx - 1, cy, 'metal_shadow')
    p(cx, cy, 'metal_dark')
    p(cx + 1, cy, 'metal_dark')
    p(cx, cy + 1, 'metal_dark')
    p(cx + 1, cy + 1, 'metal_outline')

# Clean, tight rubber cap rim at bottom-left:
# y=38
p(5, 38, 'metal_outline')
p(6, 38, 'metal_mid')
p(7, 38, 'metal_mid')
p(8, 38, 'metal_shadow')
p(9, 38, 'metal_shadow')
p(10, 38, 'metal_dark')
p(11, 38, 'metal_outline')

# y=39
p(4, 39, 'metal_outline')
p(5, 39, 'metal_hi')
p(6, 39, 'metal_mid')
p(7, 39, 'metal_mid')
p(8, 39, 'metal_shadow')
p(9, 39, 'metal_dark')
p(10, 39, 'metal_outline')

# y=40
p(5, 40, 'metal_outline')
p(6, 40, 'metal_hi')
p(7, 40, 'metal_mid')
p(8, 40, 'metal_shadow')
p(9, 40, 'metal_dark')
p(10, 40, 'metal_outline')

# y=41
p(5, 41, 'metal_outline')
p(6, 41, 'metal_mid')
p(7, 41, 'metal_shadow')
p(8, 41, 'metal_dark')
p(9, 41, 'metal_outline')

# y=42
p(6, 42, 'metal_outline')
p(7, 42, 'metal_shadow')
p(8, 42, 'metal_dark')
p(9, 42, 'metal_outline')

# y=43
p(7, 43, 'metal_outline')
p(8, 43, 'metal_outline')

# ========================================================
# 7. FRONT MUZZLE & RUBBER BUMPER (Передний срез)
# ========================================================
# Front metal band behind rubber ring (t = 34)
cx = 6 + 34; cy = 41 - 34 # (40, 7)
p(cx - 1, cy - 2, 'metal_mid_hi')
p(cx - 2, cy - 1, 'metal_mid_hi')
p(cx + 2, cy + 1, 'metal_outline')
p(cx + 1, cy + 2, 'metal_outline')
p(42, 7, 'tube_outline')

# Flared rubber bumper (t = 35..36)
# y=2
p(40, 2, 'metal_outline')
p(41, 2, 'metal_hi')
p(42, 2, 'metal_mid_hi')
p(43, 2, 'metal_outline')

# y=3
p(39, 3, 'metal_outline')
p(40, 3, 'metal_hi')
p(41, 3, 'metal_mid_hi')
p(42, 3, 'metal_mid')
p(43, 3, 'metal_shadow')
p(44, 3, 'metal_outline')

# y=4
p(39, 4, 'metal_outline')
p(40, 4, 'metal_hi')
p(41, 4, 'metal_mid')
p(42, 4, 'metal_shadow')
p(43, 4, 'metal_dark')
p(44, 4, 'metal_outline')

# y=5
p(39, 5, 'metal_outline')
p(40, 5, 'metal_mid')
p(41, 5, 'metal_shadow')
p(42, 5, 'metal_dark')
p(43, 5, 'metal_dark')
p(44, 5, 'metal_outline')

# y=6
p(40, 6, 'metal_outline')
p(41, 6, 'metal_shadow')
p(42, 6, 'metal_dark')
p(43, 6, 'metal_outline')

# Bore interior cavity (the dark rocket chamber at x=41..42, y=3..4)
p(41, 3, 'metal_dark')
p(42, 3, 'metal_dark')
p(41, 4, 'metal_dark')

# ========================================================
# 8. RAISED FRONT SIGHT (Поднятая мушка / рамка)
# ========================================================
# Hinged firmly to the front collar at (38, 5) and (38, 6)
p(38, 6, 'metal_outline')
p(38, 5, 'metal_hi')
p(37, 5, 'metal_outline')

# Ladder frame uprights:
ladder = [
    # y, left_x, right_x, mid_x
    (4, 35, 37, 36),
    (3, 34, 36, 35),
    (2, 33, 35, 34),
    (1, 32, 34, 33),
]

for y, lx, rx, mx in ladder:
    p(lx - 1, y, 'metal_outline')
    p(rx + 1, y, 'metal_outline')
    p(lx, y, 'metal_hi')
    p(rx, y, 'metal_mid')
    if y in [2, 4]:
        p(mx, y, 'metal_mid_hi') # crossbar range rungs
    else:
        p_raw(mx, y, PAL['sight_glass']) # clear aperture cutout

# Top protective arch of the ladder sight (y = 0)
p(31, 0, 'metal_outline')
p(32, 0, 'metal_spec') # Top glint
p(33, 0, 'metal_hi')
p(34, 0, 'metal_outline')

# Save final sprite
img = Image.fromarray(canvas, 'RGBA')
img.save('scratch/rpg26_final.png')
print('Final sprite generated! Bbox:', img.getbbox())
