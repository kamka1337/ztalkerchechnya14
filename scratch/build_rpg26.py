import numpy as np
from PIL import Image

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

PAL = {
    # Tube Olive
    'tube_spec':      hex_to_rgba('a0b55e'),
    'tube_hi':        hex_to_rgba('86994c'),
    'tube_mid_hi':    hex_to_rgba('6b7d3a'),
    'tube_mid':       hex_to_rgba('53622b'),
    'tube_shadow':    hex_to_rgba('3a461d'),
    'tube_dark':      hex_to_rgba('252e12'),
    'tube_outline':   hex_to_rgba('131808'),

    # Metal / Rubber (Flanges, sights, brackets)
    'metal_hi':       hex_to_rgba('626874'),
    'metal_mid_hi':   hex_to_rgba('454a53'),
    'metal_mid':      hex_to_rgba('2d3037'),
    'metal_shadow':   hex_to_rgba('1c1e23'),
    'metal_dark':     hex_to_rgba('121317'),
    'metal_outline':  hex_to_rgba('0a0b0e'),

    # Steel rod
    'rod_hi':         hex_to_rgba('9ca599'),
    'rod_mid':        hex_to_rgba('6e776b'),
    'rod_shadow':     hex_to_rgba('474e44'),

    # Placard
    'placard_bg':     hex_to_rgba('16181b'),
    'placard_border': hex_to_rgba('22262a'),
    'arrow_white':    hex_to_rgba('ffffff'),
    'text_white':     hex_to_rgba('e2e6e2'),
    'text_dim':       hex_to_rgba('889089'),

    # Red safety / cocking accent
    'red_bright':     hex_to_rgba('e83830'),
    'red_dark':       hex_to_rgba('8c1813'),

    # Translucent glass / diopter opening
    'sight_glass':    hex_to_rgba('688899', 90),
}

canvas = np.zeros((48, 48, 4), dtype=np.uint8)

def p(x, y, color):
    if 0 <= x < 48 and 0 <= y < 48:
        canvas[y, x] = PAL[color]

def p_raw(x, y, color_rgba):
    if 0 <= x < 48 and 0 <= y < 48:
        canvas[y, x] = color_rgba

# ==========================================
# 1. MAIN TUBE BODY & CYLINDER
# ==========================================
# Tube runs from t = 5 (rear) to t = 34 (front)
# For each step t, axis is (cx, cy)
# cx = 6 + t, cy = 41 - t
# Let's draw the layers from top-left to bottom-right:
for t in range(5, 34):
    cx = 6 + t
    cy = 41 - t

    # Top outline
    p(cx - 1, cy - 2, 'tube_outline')
    p(cx - 2, cy - 1, 'tube_outline')

    # Cylinder highlight
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

# Specular glints along top highlight (subtle gloss reflection)
for t in range(7, 32, 2):
    cx = 6 + t
    cy = 41 - t
    p(cx - 1, cy - 1, 'tube_spec')

# ==========================================
# 2. INSTRUCTION PLACARD (табличка)
# ==========================================
# Located around t = 15 to 22 (middle-front side of the tube)
# Sits on the side face: (cx, cy-1), (cx, cy), (cx+1, cy), (cx, cy+1)
for t in range(15, 23):
    cx = 6 + t
    cy = 41 - t
    p(cx, cy - 1, 'placard_border')
    p(cx - 1, cy, 'placard_border')
    p(cx, cy, 'placard_bg')
    p(cx + 1, cy, 'placard_bg')
    p(cx, cy + 1, 'placard_bg')
    p(cx + 1, cy + 1, 'placard_border')

# White Directional Arrow (<--- towards muzzle, top-right)
# Arrow point at t = 21, shaft at t = 18..20
t = 21; cx = 6 + t; cy = 41 - t
p(cx, cy, 'arrow_white')
p(cx + 1, cy, 'arrow_white')

t = 20; cx = 6 + t; cy = 41 - t
p(cx, cy, 'arrow_white')
p(cx + 1, cy, 'text_dim')

t = 19; cx = 6 + t; cy = 41 - t
p(cx, cy, 'text_white')
p(cx + 1, cy, 'text_dim')

t = 18; cx = 6 + t; cy = 41 - t
p(cx, cy, 'text_dim')
p(cx + 1, cy, 'text_white')

# White text blocks (instruction schematics)
for t in [15, 16, 17]:
    cx = 6 + t; cy = 41 - t
    p(cx, cy, 'text_white' if t % 2 == 0 else 'text_dim')
    p(cx + 1, cy, 'text_dim' if t % 2 == 0 else 'text_white')

# ==========================================
# 3. TRIGGER HOUSING & REAR SIGHT BASE (УСМ)
# ==========================================
# Located at t = 12..14
for t in [12, 13, 14]:
    cx = 6 + t; cy = 41 - t
    # Clamp collar around tube
    p(cx - 2, cy - 1, 'metal_outline')
    p(cx - 1, cy - 2, 'metal_outline')
    p(cx - 1, cy - 1, 'metal_mid_hi')
    p(cx + 2, cy + 1, 'metal_outline')
    p(cx + 1, cy + 2, 'metal_outline')
    # Trigger housing box on top-left of tube
    p(cx - 2, cy - 2, 'metal_mid')
    p(cx - 3, cy - 2, 'metal_outline')
    p(cx - 2, cy - 3, 'metal_outline')

# Red safety / cocking flag on the trigger box
cx = 6 + 14; cy = 41 - 14
p(cx - 2, cy - 2, 'red_bright')
p(cx - 1, cy - 2, 'red_dark')

# ==========================================
# 4. RAISED REAR SIGHT (Поднятая стойка прицела)
# ==========================================
# Rises upright (up-left) from the trigger housing at t = 13
# Base is at (cx-2, cy-2) = (17, 26)
# Stalk goes up vertically/diagonally:
p(17, 25, 'metal_outline')
p(17, 24, 'metal_mid')
p(16, 24, 'metal_hi')
p(15, 24, 'metal_outline')

p(17, 23, 'metal_shadow')
p(16, 23, 'metal_mid_hi')
p(15, 23, 'metal_outline')

p(17, 22, 'metal_outline')
p(16, 22, 'metal_mid')
p(15, 22, 'metal_hi')
p(14, 22, 'metal_outline')

# Sight head / diopter aperture at top (y = 20, 21)
p(16, 21, 'metal_outline')
p(15, 21, 'metal_hi')
p(14, 21, 'metal_outline')

p(16, 20, 'metal_outline')
p(15, 20, 'metal_hi') # peep sight top notch
p(14, 20, 'metal_outline')

# ==========================================
# 5. LINKAGE ROD (Тяга спускового механизма)
# ==========================================
# Runs along the top edge from rear (t = 6) to trigger housing (t = 12)
for t in range(6, 13):
    cx = 6 + t; cy = 41 - t
    p(cx - 2, cy - 2, 'rod_hi')
    p(cx - 1, cy - 3, 'rod_shadow')
    p(cx - 3, cy - 1, 'rod_shadow')

# ==========================================
# 6. REAR CAP & EXHAUST BELL (Казённая часть / сопло)
# ==========================================
# t = 1..4
for t in [3, 4]:
    cx = 6 + t; cy = 41 - t
    # Rear collar band
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

# Rear black rubber end-cap (t = 1..2)
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

# Rear end rim and flange lock bracket
p(6, 40, 'metal_hi')
p(6, 41, 'metal_mid')
p(5, 41, 'metal_outline')
p(7, 42, 'metal_dark')
p(6, 42, 'metal_outline')
p(7, 43, 'metal_outline')

# ==========================================
# 7. FRONT MUZZLE & RUBBER BUMPER (Передний срез)
# ==========================================
# t = 32..35
for t in [32, 33]:
    cx = 6 + t; cy = 41 - t
    # Front metal band
    p(cx - 1, cy - 2, 'metal_mid_hi')
    p(cx - 2, cy - 1, 'metal_mid_hi')
    p(cx + 2, cy + 1, 'metal_outline')
    p(cx + 1, cy + 2, 'metal_outline')

# Flared front rubber bumper ring (t = 34, 35)
for t in [34, 35]:
    cx = 6 + t; cy = 41 - t
    # Flared outer diameter (wider than tube)
    p(cx - 3, cy - 2, 'metal_outline')
    p(cx - 2, cy - 3, 'metal_outline')
    p(cx - 2, cy - 2, 'metal_hi')
    p(cx - 1, cy - 2, 'metal_mid_hi')
    p(cx - 2, cy - 1, 'metal_mid_hi')
    p(cx - 1, cy - 1, 'metal_mid')
    p(cx, cy - 1, 'metal_mid')
    p(cx - 1, cy, 'metal_mid')
    p(cx, cy, 'metal_shadow')
    p(cx + 1, cy, 'metal_dark')
    p(cx, cy + 1, 'metal_dark')
    p(cx + 1, cy + 1, 'metal_dark')
    p(cx + 2, cy + 1, 'metal_outline')
    p(cx + 1, cy + 2, 'metal_outline')
    p(cx + 2, cy + 2, 'metal_outline')

# Front nozzle aperture / bore face (t = 36)
cx = 6 + 36; cy = 41 - 36 # (42, 5)
p(cx - 2, cy - 1, 'metal_outline')
p(cx - 1, cy - 2, 'metal_outline')
p(cx - 1, cy - 1, 'metal_hi')
p(cx, cy - 1, 'metal_dark')
p(cx - 1, cy, 'metal_dark')
p(cx, cy, 'metal_outline')
p(cx + 1, cy, 'metal_outline')
p(cx, cy + 1, 'metal_outline')

# ==========================================
# 8. RAISED FRONT SIGHT (Поднятая мушка / диоптрическая рамка)
# ==========================================
# Mounted at t = 32 (cx = 38, cy = 9)
# Rises upright (up-left) like a tall ladder frame!
# Base hinge on the tube:
p(37, 7, 'metal_outline')
p(36, 7, 'metal_hi')
p(35, 7, 'metal_outline')

# Ladder frame uprights:
# Right upright (inner): x = 36..35
# Left upright (outer): x = 33..32
# Inside is the sight window / scale apertures!
ladder_heights = [
    # y, left_col, right_col, mid_window
    (6, 33, 35, 34),
    (5, 33, 35, 34),
    (4, 32, 34, 33),
    (3, 32, 34, 33),
    (2, 31, 33, 32),
    (1, 31, 33, 32),
]

for y, l_x, r_x, m_x in ladder_heights:
    # Outer outlines
    p(l_x - 1, y, 'metal_outline')
    p(r_x + 1, y, 'metal_outline')
    # Frame uprights
    p(l_x, y, 'metal_hi')
    p(r_x, y, 'metal_mid')
    # Internal window / diopter slots:
    # alternate between clear slot (empty or translucent) and crossbar notch
    if y in [2, 4, 6]:
        # Crossbar notch (деления шкалы)
        p(m_x, y, 'metal_mid_hi')
    else:
        # Open aiming window (clear opening so it looks like a real ladder sight!)
        p_raw(m_x, y, PAL['sight_glass'])

# Top protective arch of the ladder sight (y = 0)
p(31, 0, 'metal_outline')
p(32, 0, 'metal_hi')
p(33, 0, 'metal_outline')

img = Image.fromarray(canvas, 'RGBA')
img.save('scratch/rpg26_test.png')
print('Sprite generated successfully. Size:', img.size)
