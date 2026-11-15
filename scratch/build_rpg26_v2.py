import numpy as np
from PIL import Image

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# Professional Soviet Military Palette
PAL = {
    # Tube Olive Drab (rich Russian military matte khaki)
    'tube_spec':      hex_to_rgba('a8be65'), # Specular glint
    'tube_hi':        hex_to_rgba('8ba04e'), # Upper highlight
    'tube_mid_hi':    hex_to_rgba('6f833c'), # Upper midtone
    'tube_mid':       hex_to_rgba('56672d'), # Base olive
    'tube_shadow':    hex_to_rgba('3e4b1e'), # Underside shadow
    'tube_dark':      hex_to_rgba('273113'), # Ambient shadow
    'tube_outline':   hex_to_rgba('141a09'), # Dark outline

    # Black / Dark Steel / Rubber
    'metal_spec':     hex_to_rgba('7b8391'), # Metal glint
    'metal_hi':       hex_to_rgba('59616e'), # Steel highlight
    'metal_mid_hi':   hex_to_rgba('404650'), # Metal upper
    'metal_mid':      hex_to_rgba('292d34'), # Rubber/metal core
    'metal_shadow':   hex_to_rgba('1a1d22'), # Dark rubber
    'metal_dark':     hex_to_rgba('121417'), # Deep shadow
    'metal_outline':  hex_to_rgba('0c0e11'), # Pure outline

    # Mechanical Steel Rod (тяга)
    'rod_hi':         hex_to_rgba('9ca59a'), # Steel highlight
    'rod_mid':        hex_to_rgba('697267'), # Steel body
    'rod_shadow':     hex_to_rgba('434a41'), # Rod shadow

    # Placard (табличка)
    'placard_bg':     hex_to_rgba('15181b'), # Deep black
    'placard_edge':   hex_to_rgba('282e34'), # Subtle edge
    'arrow_white':    hex_to_rgba('ffffff'), # Bright arrow
    'text_white':     hex_to_rgba('e2e8e3'), # White schematic text
    'text_dim':       hex_to_rgba('89938a'), # Dim text

    # Safety Red Accent (предохранитель)
    'red_bright':     hex_to_rgba('eb362d'), # Safety red
    'red_dark':       hex_to_rgba('8a1a15'), # Shadow red

    # Sight diopter aperture (window cutout)
    'sight_glass':    hex_to_rgba('6a889c', 75), # Very subtle glass tint
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
# Axis runs from rear t=5 to front t=34
# For t in range(5, 35):
# cx = 6 + t, cy = 41 - t
for t in range(5, 35):
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

# Specular edge glints
for t in range(6, 34, 2):
    cx = 6 + t; cy = 41 - t
    p(cx - 1, cy - 1, 'tube_spec')

# ========================================================
# 2. LINKAGE ROD (Тяга спускового механизма)
# ========================================================
# Runs along the top from t=5 (rear) to t=21 (trigger block)
for t in range(5, 22):
    cx = 6 + t; cy = 41 - t
    p(cx - 2, cy - 2, 'rod_hi')
    p(cx - 1, cy - 3, 'rod_shadow')
    p(cx - 3, cy - 1, 'rod_shadow')

# Rear rod mount bracket at t=5
cx = 6 + 5; cy = 41 - 5
p(cx - 2, cy - 2, 'metal_mid_hi')
p(cx - 3, cy - 2, 'metal_outline')
p(cx - 2, cy - 3, 'metal_outline')

# ========================================================
# 3. TRIGGER HOUSING & REAR SIGHT BASE (t = 21..24)
# ========================================================
# Trigger block and collar clamping around the tube
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

    # Raised housing box on top-left
    p(cx - 2, cy - 2, 'metal_mid')
    p(cx - 3, cy - 2, 'metal_shadow')
    p(cx - 2, cy - 3, 'metal_shadow')
    p(cx - 3, cy - 3, 'metal_outline')

# Red safety / cocking indicator flag (on the trigger box at t=21)
cx = 6 + 21; cy = 41 - 21
p(cx - 2, cy - 2, 'red_bright')
p(cx - 1, cy - 2, 'red_dark')

# ========================================================
# 4. RAISED REAR SIGHT (Поднятая стойка прицела)
# ========================================================
# Erected vertically/perpendicularly from the trigger block at t = 22
# Base: (cx-2, cy-2) where cx = 28, cy = 19 -> base at (26, 17)
# Stalk rises upright towards top-left:
# Layer 1 (y = 16)
p(25, 16, 'metal_outline')
p(26, 16, 'metal_mid_hi')
p(27, 16, 'metal_shadow')
p(28, 16, 'metal_outline')

# Layer 2 (y = 15)
p(25, 15, 'metal_outline')
p(26, 15, 'metal_hi')
p(27, 15, 'metal_mid')
p(28, 15, 'metal_outline')

# Layer 3 (y = 14) - Sight head begins
p(24, 14, 'metal_outline')
p(25, 14, 'metal_hi')
p(26, 14, 'metal_mid')
p(27, 14, 'metal_outline')

# Layer 4 (y = 13) - Diopter aperture level
p(24, 13, 'metal_outline')
p(25, 13, 'metal_spec') # Left blade
p(26, 13, 'metal_outline') # Peep aperture / hole!
p(27, 13, 'metal_hi') # Right blade
p(28, 13, 'metal_outline')

# Layer 5 (y = 12) - Top protective hood / notch
p(24, 12, 'metal_outline')
p(25, 12, 'metal_hi')
p(26, 12, 'metal_spec') # Top notch center
p(27, 12, 'metal_hi')
p(28, 12, 'metal_outline')

# Top cap (y = 11)
p(25, 11, 'metal_outline')
p(26, 11, 'metal_outline')

# ========================================================
# 5. INSTRUCTION PLACARD (Табличка с инструкцией)
# ========================================================
# Located right next to trigger housing, along side of tube: t = 14 to 21
for t in range(14, 21):
    cx = 6 + t; cy = 41 - t
    p(cx, cy - 1, 'placard_edge')
    p(cx - 1, cy, 'placard_edge')
    p(cx, cy, 'placard_bg')
    p(cx + 1, cy, 'placard_bg')
    p(cx, cy + 1, 'placard_bg')
    p(cx + 1, cy + 1, 'placard_edge')

# Directional white arrow pointing to top-right (muzzle)
# Arrow head at t = 20
t = 20; cx = 6 + t; cy = 41 - t
p(cx, cy, 'arrow_white')
p(cx + 1, cy, 'arrow_white')

# Arrow shaft at t = 19
t = 19; cx = 6 + t; cy = 41 - t
p(cx, cy, 'arrow_white')
p(cx + 1, cy, 'placard_bg')

# White text / diagram lines at t = 15..18
for t in [15, 16, 17, 18]:
    cx = 6 + t; cy = 41 - t
    p(cx, cy, 'text_white' if t % 2 == 0 else 'text_dim')
    p(cx + 1, cy, 'text_dim' if t % 2 == 0 else 'text_white')

# ========================================================
# 6. REAR END-CAP & EXHAUST BELL (Казённая часть / сопло)
# ========================================================
# t = 1..4 (bottom-left)
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

# Rear rubber cap (t = 1..2)
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
    p(cx + 2, cy + 2, 'metal_outline')

# Rear flange locking rim (t = 0..1)
p(6, 40, 'metal_hi')
p(5, 41, 'metal_outline')
p(6, 41, 'metal_mid')
p(6, 42, 'metal_shadow')
p(7, 42, 'metal_dark')
p(5, 42, 'metal_outline')
p(6, 43, 'metal_outline')
p(7, 43, 'metal_outline')

# ========================================================
# 7. FRONT MUZZLE & RUBBER BUMPER (Передний срез ствола)
# ========================================================
# Metal band behind bumper: t = 34
cx = 6 + 34; cy = 41 - 34 # (40, 7)
p(cx - 1, cy - 2, 'metal_mid_hi')
p(cx - 2, cy - 1, 'metal_mid_hi')
p(cx + 2, cy + 1, 'metal_outline')
p(cx + 1, cy + 2, 'metal_outline')

# Flared front rubber bumper (t = 35..36)
for t in [35, 36]:
    cx = 6 + t; cy = 41 - t
    p(cx - 3, cy - 2, 'metal_outline')
    p(cx - 2, cy - 3, 'metal_outline')
    p(cx - 2, cy - 2, 'metal_hi')
    p(cx - 1, cy - 2, 'metal_mid_hi')
    p(cx - 2, cy - 1, 'metal_mid_hi')
    p(cx - 1, cy - 1, 'metal_mid')
    p(cx, cy - 1, 'metal_mid')
    p(cx - 1, cy, 'metal_mid')
    p(cx, cy, 'metal_shadow')
    p(cx + 1, cy, 'metal_shadow')
    p(cx, cy + 1, 'metal_shadow')
    p(cx + 1, cy + 1, 'metal_dark')
    p(cx + 2, cy + 1, 'metal_outline')
    p(cx + 1, cy + 2, 'metal_outline')
    p(cx + 2, cy + 2, 'metal_outline')

# Front nozzle bore aperture (t = 37)
cx = 6 + 37; cy = 41 - 37 # (43, 4)
p(cx - 2, cy - 1, 'metal_outline')
p(cx - 1, cy - 2, 'metal_outline')
p(cx - 1, cy - 1, 'metal_hi')
p(cx, cy - 1, 'metal_shadow')
p(cx - 1, cy, 'metal_shadow')
p(cx, cy, 'metal_outline')
p(cx + 1, cy, 'metal_outline')
p(cx, cy + 1, 'metal_outline')

# ========================================================
# 8. RAISED FRONT SIGHT (Поднятая мушка / рамка прицела)
# ========================================================
# Hinged at t = 34 right behind the front rubber bumper
# Base hinge firmly attached to the collar at (cx-1, cy-2) = (39, 5)
p(39, 5, 'metal_outline')
p(38, 5, 'metal_hi')
p(37, 5, 'metal_outline')

# Ladder frame rises upright (up-left)
# Height levels from base (y=4) to top (y=0)
ladder = [
    # y, left_x, right_x, mid_x
    (4, 35, 37, 36),
    (3, 34, 36, 35),
    (2, 33, 35, 34),
    (1, 32, 34, 33),
]

for y, lx, rx, mx in ladder:
    # Outer outlines
    p(lx - 1, y, 'metal_outline')
    p(rx + 1, y, 'metal_outline')
    # Uprights
    p(lx, y, 'metal_hi')
    p(rx, y, 'metal_mid')
    # Internal window / diopter slots
    if y in [2, 4]:
        # Crossbar rung / range marking
        p(mx, y, 'metal_mid_hi')
    else:
        # Clear aperture window inside the frame
        p_raw(mx, y, PAL['sight_glass'])

# Top protective arch of the ladder sight (y = 0)
p(31, 0, 'metal_outline')
p(32, 0, 'metal_spec') # Top glint
p(33, 0, 'metal_hi')
p(34, 0, 'metal_outline')

# Save output
img = Image.fromarray(canvas, 'RGBA')
img.save('scratch/rpg26_detailed.png')
print('Detailed sprite created! Bbox:', img.getbbox())
