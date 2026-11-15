import numpy as np
from PIL import Image, ImageDraw

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# EXACT PALETTE EXTRACTED DIRECTLY FROM USER'S SPRITESHEET (DVL-10, VSS, AK103)
PAL = {
    # Tube Olive Green (exact from DVL-10 in sheet)
    'g_top_rim': hex_to_rgba('3b432b'), # Top-left rim / shadow edge
    'g_light':   hex_to_rgba('576341'), # Light olive face
    'g_spec':    hex_to_rgba('69774a'), # Specular highlight stripe
    'g_mid':     hex_to_rgba('495236'), # Core body olive
    'g_dark':    hex_to_rgba('303722'), # Deep underside shadow rim

    # Metal / Dark Rubber / Receiver (exact from AK103 & VSS in sheet)
    'm_spec':    hex_to_rgba('4b505a'), # Steel glint
    'm_hi':      hex_to_rgba('383a3c'), # Metal highlight
    'm_mid_hi':  hex_to_rgba('303133'), # Metal upper-mid
    'm_mid':     hex_to_rgba('242629'), # Metal core
    'm_shadow':  hex_to_rgba('1c1c1c'), # Dark metal / rubber
    'm_dark':    hex_to_rgba('16171b'), # Deep dark metal
    'm_deep':    hex_to_rgba('0b0c0d'), # Near black
    'outline':   hex_to_rgba('000000'), # Pure black outline from sheet

    # Linkage rod (тяга)
    'rod_hi':    hex_to_rgba('383a3c'),
    'rod_mid':   hex_to_rgba('242629'),

    # Placard (табличка)
    'pl_bg':     hex_to_rgba('16171b'), # Black label background
    'pl_arrow':  hex_to_rgba('ffffff'), # Pure white arrow
    'pl_text':   hex_to_rgba('cbd0cb'), # White-gray text line
    'pl_dim':    hex_to_rgba('889089'), # Dim text line

    # Cocking / Safety Red Accent (from sheet)
    'red_bright':hex_to_rgba('9a2b2b'), # Red safety latch
    'red_dark':  hex_to_rgba('83262c'),
}

canvas = np.zeros((48, 48, 4), dtype=np.uint8)

def p(x, y, color):
    if 0 <= x < 48 and 0 <= y < 48:
        canvas[y, x] = PAL[color]

# ========================================================
# 1. LAUNCH TUBE BODY (Пусковая труба)
# ========================================================
# Exact 45-degree diagonal matching DVL-10 and VSS:
# Scanlines y from 11 (front) to 35 (rear):
for y in range(11, 36):
    cx = 48 - y  # cx moves from 37 down to 13
    
    # Clean 5-pixel cylinder cross section without black outline (pure DVL-10 style):
    p(cx - 2, y, 'g_top_rim') # Upper rim
    p(cx - 1, y, 'g_light')   # Upper face
    p(cx,     y, 'g_spec')    # Highlight stripe
    p(cx + 1, y, 'g_mid')     # Core olive
    p(cx + 2, y, 'g_dark')    # Underside shadow rim

# ========================================================
# 2. INSTRUCTION PLACARD (Табличка с инструкцией)
# ========================================================
# In the middle section: y = 21 to 26
# Sits cleanly on the side face of the tube (cx, cx+1, cx+2)
for y in range(21, 27):
    cx = 48 - y
    p(cx,     y, 'pl_bg')
    p(cx + 1, y, 'pl_bg')
    p(cx + 2, y, 'pl_bg')

# White Directional Arrow pointing to top-right (muzzle)
cx = 48 - 22 # cx = 26
p(cx + 1, 22, 'pl_arrow')
p(cx + 2, 22, 'pl_arrow')

cx = 48 - 23 # cx = 25
p(cx + 1, 23, 'pl_arrow')

# Instruction schematic lines
cx = 48 - 24 # cx = 24
p(cx + 1, 24, 'pl_text')
p(cx + 2, 24, 'pl_dim')

cx = 48 - 25 # cx = 23
p(cx + 1, 25, 'pl_dim')
p(cx + 2, 25, 'pl_text')

# ========================================================
# 3. TRIGGER HOUSING & REAR SIGHT (УСМ и стойка прицела)
# ========================================================
# Collar band wrapping the tube at y = 20
cx = 48 - 20 # 28
p(cx - 3, 20, 'outline')
p(cx - 2, 20, 'm_hi')
p(cx - 1, 20, 'm_mid')
p(cx,     20, 'm_shadow')
p(cx + 1, 20, 'm_dark')
p(cx + 2, 20, 'outline')

# Trigger housing box sitting at y = 19, x = 26..28
p(25, 19, 'outline')
p(26, 19, 'm_hi')
p(27, 19, 'm_mid')
p(28, 19, 'outline')

# Red safety / cocking indicator flag (on the trigger box)
p(25, 20, 'red_bright')
p(26, 20, 'red_dark')

# RAISED REAR SIGHT (Поднятая стойка прицела)
# Stands upright 4 pixels tall above the trigger housing (y = 15..18, x = 26..28)
# y = 18 (base stalk)
p(26, 18, 'outline')
p(27, 18, 'm_hi')
p(28, 18, 'outline')

# y = 17 (mid stalk)
p(26, 17, 'outline')
p(27, 17, 'm_hi')
p(28, 17, 'outline')

# y = 16 (diopter sight head with aperture notch)
p(25, 16, 'outline')
p(26, 16, 'm_spec') # left blade
p(27, 16, 'm_deep') # peep aperture hole!
p(28, 16, 'm_hi')   # right blade
p(29, 16, 'outline')

# y = 15 (top sighting notch)
p(26, 15, 'outline')
p(27, 15, 'm_hi')
p(28, 15, 'outline')

# ========================================================
# 4. LINKAGE ROD (Тяга спускового механизма)
# ========================================================
# Sleek 1-pixel steel rod along top edge of tube from y = 22 down to y = 33
for y in range(21, 34):
    cx = 48 - y
    p(cx - 3, y, 'rod_hi')

# Bracket at rear of linkage rod: y = 33, 34
p(11, 33, 'm_hi')
p(11, 34, 'm_mid')
p(10, 33, 'outline')
p(10, 34, 'outline')

# ========================================================
# 5. REAR END CAP & EXHAUST BELL (Казённая часть / сопло)
# ========================================================
# Flared rubber cap at y = 35..39, x = 8..15
# y = 35 (rear collar)
p(10, 35, 'outline')
p(11, 35, 'm_hi')
p(12, 35, 'm_mid')
p(13, 35, 'm_shadow')
p(14, 35, 'm_dark')
p(15, 35, 'outline')

# y = 36 (rubber cap flange)
p(9,  36, 'outline')
p(10, 36, 'm_hi')
p(11, 36, 'm_mid')
p(12, 36, 'm_shadow')
p(13, 36, 'm_dark')
p(14, 36, 'outline')

# y = 37 (rear cap body)
p(8,  37, 'outline')
p(9,  37, 'm_hi')
p(10, 37, 'm_mid')
p(11, 37, 'm_shadow')
p(12, 37, 'm_dark')
p(13, 37, 'outline')

# y = 38 (rounded rear rim)
p(8,  38, 'outline')
p(9,  38, 'm_mid')
p(10, 38, 'm_shadow')
p(11, 38, 'm_dark')
p(12, 38, 'outline')

# y = 39 (bottom outline of cap)
p(9,  39, 'outline')
p(10, 39, 'outline')
p(11, 39, 'outline')

# ========================================================
# 6. FRONT MUZZLE & RUBBER BUMPER (Передний срез ствола)
# ========================================================
# Metal band behind bumper at y = 11
cx = 48 - 11 # 37
p(cx - 3, 11, 'outline')
p(cx - 2, 11, 'm_hi')
p(cx - 1, 11, 'm_mid')
p(cx,     11, 'm_shadow')
p(cx + 1, 11, 'm_dark')
p(cx + 2, 11, 'outline')

# Flared rubber bumper ring at y = 8..10
# y = 10
p(36, 10, 'outline')
p(37, 10, 'm_hi')
p(38, 10, 'm_mid')
p(39, 10, 'm_shadow')
p(40, 10, 'm_dark')
p(41, 10, 'outline')

# y = 9 (front face / muzzle rim)
p(37, 9, 'outline')
p(38, 9, 'm_spec')
p(39, 9, 'm_hi')
p(40, 9, 'm_shadow')
p(41, 9, 'm_dark')
p(42, 9, 'outline')

# y = 8 (front opening / rocket bore)
p(38, 8, 'outline')
p(39, 8, 'm_hi')
p(40, 8, 'm_deep') # dark bore hole
p(41, 8, 'm_shadow')
p(42, 8, 'outline')

# y = 7 (tip outline)
p(39, 7, 'outline')
p(40, 7, 'outline')
p(41, 7, 'outline')

# ========================================================
# 7. RAISED FRONT SIGHT (Поднятая мушка-диоптр)
# ========================================================
# Hinged right behind the front bumper collar at y = 11
# Sits at x = 34..36. Stands upright 4 pixels tall (y = 6..9)
# Base hinge bracket at y = 10
p(35, 10, 'm_hi')

# Ladder frame:
# y = 9 (lower frame)
p(34, 9, 'outline')
p(35, 9, 'm_hi')
p(36, 9, 'outline')

# y = 8 (frame with central aiming slot opening)
p(33, 8, 'outline')
p(34, 8, 'm_hi')   # left upright
p(35, 8, 'm_dark') # open viewing window / slot!
p(36, 8, 'm_mid')  # right upright
p(37, 8, 'outline')

# y = 7 (range crossbar notch)
p(33, 7, 'outline')
p(34, 7, 'm_spec') # notch highlight
p(35, 7, 'm_hi')
p(36, 7, 'm_mid')
p(37, 7, 'outline')

# y = 6 (top protective arch / hood)
p(33, 6, 'outline')
p(34, 6, 'm_spec') # top apex glint
p(35, 6, 'm_hi')
p(36, 6, 'outline')

# Top tip
p(34, 5, 'outline')
p(35, 5, 'outline')

img = Image.fromarray(canvas, 'RGBA')
img.save('scratch/rpg26_perfect.png')
print('Perfect RPG-26 generated! Bbox:', img.getbbox())
