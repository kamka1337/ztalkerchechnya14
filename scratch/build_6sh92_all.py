import numpy as np
from PIL import Image

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# Server-matching military palette for 6Sh92
PAL = {
    # 6Sh92 Flora / VSR-93 Camouflage Palette
    'olv_hi':      hex_to_rgba('69774a'), # Specular / upper highlight
    'olv_light':   hex_to_rgba('576341'), # Light olive cloth
    'olv_mid':     hex_to_rgba('495236'), # Core olive cloth
    'olv_shadow':  hex_to_rgba('3b432b'), # Shaded olive cloth
    'olv_dark':    hex_to_rgba('303722'), # Deep fold shadow
    
    # Camouflage brown / khaki spots (Flora pattern)
    'camo_light':  hex_to_rgba('6e5b42'), # Khaki spot
    'camo_mid':    hex_to_rgba('564430'), # Brown spot
    'camo_dark':   hex_to_rgba('3d2e1f'), # Dark brown spot

    # Webbing straps (регулировочные стропы)
    'strap_hi':    hex_to_rgba('424b35'),
    'strap_mid':   hex_to_rgba('323a26'),
    'strap_dark':  hex_to_rgba('222818'),

    # Metal Hardware (Buckles, D-rings, snaps, eyelets)
    'metal_spec':  hex_to_rgba('828c9b'), # Metal glint
    'metal_hi':    hex_to_rgba('59616e'), # Steel highlight
    'metal_mid':   hex_to_rgba('383e47'), # Steel midtone
    'metal_dark':  hex_to_rgba('202329'), # Steel shadow

    # Black trim / Fastex plastic / Pull-tabs
    'fastex_hi':   hex_to_rgba('363940'),
    'fastex_mid':  hex_to_rgba('202227'),
    'fastex_dark': hex_to_rgba('121417'),

    # Outlines
    'outline':     hex_to_rgba('0a0c0e'), # Crisp black outline
    'dark_crevice':hex_to_rgba('15181b'), # Deep seam
}

# ========================================================
# 1. ICON (32x32)
# ========================================================
import sys
sys.path.append('.')
from scratch.build_6sh92_icon import create_icon

# ========================================================
# 2. EQUIPPED-BELT (64x64, 4 directions)
# ========================================================
def create_equipped_belt():
    canvas = np.zeros((64, 64, 4), dtype=np.uint8)

    def p(qx, qy, x, y, color):
        px = qx + x
        py = qy + y
        if 0 <= px < 64 and 0 <= py < 64:
            canvas[py, px] = PAL[color]

    # --- QUADRANT 1: SOUTH (Front View, 0, 0) ---
    qx, qy = 0, 0
    # Shoulders (y = 10, 11)
    p(qx, qy, 11, 10, 'outline'); p(qx, qy, 12, 10, 'olv_hi'); p(qx, qy, 13, 10, 'outline')
    p(qx, qy, 18, 10, 'outline'); p(qx, qy, 19, 10, 'olv_light'); p(qx, qy, 20, 10, 'outline')

    # D-Rings on shoulders (y = 11)
    p(qx, qy, 11, 11, 'outline'); p(qx, qy, 12, 11, 'metal_spec'); p(qx, qy, 13, 11, 'outline')
    p(qx, qy, 18, 11, 'outline'); p(qx, qy, 19, 11, 'metal_spec'); p(qx, qy, 20, 11, 'outline')

    # Chest & Upper pouches (y = 12..14)
    # y = 12
    p(qx, qy, 11, 12, 'outline'); p(qx, qy, 12, 12, 'olv_light'); p(qx, qy, 13, 12, 'camo_light'); p(qx, qy, 14, 12, 'outline')
    p(qx, qy, 15, 12, 'fastex_hi'); p(qx, qy, 16, 12, 'fastex_mid')
    p(qx, qy, 17, 12, 'outline'); p(qx, qy, 18, 12, 'camo_mid'); p(qx, qy, 19, 12, 'olv_mid'); p(qx, qy, 20, 12, 'outline')

    # y = 13 (Bayonet sheath on left chest, utility pouch on right)
    p(qx, qy, 11, 13, 'outline'); p(qx, qy, 12, 13, 'metal_hi'); p(qx, qy, 13, 13, 'strap_mid'); p(qx, qy, 14, 13, 'outline')
    p(qx, qy, 15, 13, 'strap_mid'); p(qx, qy, 16, 13, 'strap_dark')
    p(qx, qy, 17, 13, 'outline'); p(qx, qy, 18, 13, 'metal_hi'); p(qx, qy, 19, 13, 'olv_shadow'); p(qx, qy, 20, 13, 'outline')

    # y = 14 (Upper flaps transition)
    p(qx, qy, 11, 14, 'outline'); p(qx, qy, 12, 14, 'olv_light'); p(qx, qy, 13, 14, 'olv_mid'); p(qx, qy, 14, 14, 'outline')
    p(qx, qy, 15, 14, 'outline'); p(qx, qy, 16, 14, 'outline')
    p(qx, qy, 17, 14, 'outline'); p(qx, qy, 18, 14, 'olv_mid'); p(qx, qy, 19, 14, 'olv_shadow'); p(qx, qy, 20, 14, 'outline')

    # Belly: 4 AK-74 Magazine Pouches & Grenade Pouches (y = 15..18)
    # y = 15 (Flaps with button snaps)
    p(qx, qy, 10, 15, 'outline') # Left grenade pouch top
    p(qx, qy, 11, 15, 'olv_hi'); p(qx, qy, 12, 15, 'metal_spec') # Mag pouch 1
    p(qx, qy, 13, 15, 'olv_light'); p(qx, qy, 14, 15, 'metal_spec') # Mag pouch 2
    p(qx, qy, 15, 15, 'outline'); p(qx, qy, 16, 15, 'outline') # Center gap
    p(qx, qy, 17, 15, 'metal_spec'); p(qx, qy, 18, 15, 'olv_light') # Mag pouch 3
    p(qx, qy, 19, 15, 'metal_spec'); p(qx, qy, 20, 15, 'olv_mid') # Mag pouch 4
    p(qx, qy, 21, 15, 'outline') # Right grenade pouch top

    # y = 16 (Pouch bodies + pull tabs)
    p(qx, qy, 10, 16, 'metal_spec') # Left grenade snap
    p(qx, qy, 11, 16, 'fastex_dark'); p(qx, qy, 12, 16, 'camo_light')
    p(qx, qy, 13, 16, 'fastex_dark'); p(qx, qy, 14, 16, 'olv_light')
    p(qx, qy, 15, 16, 'fastex_hi'); p(qx, qy, 16, 16, 'fastex_mid') # Lower center buckle
    p(qx, qy, 17, 16, 'olv_mid'); p(qx, qy, 18, 16, 'fastex_dark')
    p(qx, qy, 19, 16, 'camo_mid'); p(qx, qy, 20, 16, 'fastex_dark')
    p(qx, qy, 21, 16, 'metal_spec') # Right grenade snap

    # y = 17 (Pouch bodies with Flora camo)
    p(qx, qy, 10, 17, 'olv_light')
    p(qx, qy, 11, 17, 'camo_light'); p(qx, qy, 12, 17, 'camo_mid')
    p(qx, qy, 13, 17, 'olv_light'); p(qx, qy, 14, 17, 'olv_mid')
    p(qx, qy, 15, 17, 'strap_mid'); p(qx, qy, 16, 17, 'strap_dark')
    p(qx, qy, 17, 17, 'camo_light'); p(qx, qy, 18, 17, 'camo_mid')
    p(qx, qy, 19, 17, 'olv_mid'); p(qx, qy, 20, 17, 'olv_shadow')
    p(qx, qy, 21, 17, 'olv_shadow')

    # y = 18 (Pouch bottom & shadows)
    p(qx, qy, 10, 18, 'olv_shadow')
    p(qx, qy, 11, 18, 'camo_mid'); p(qx, qy, 12, 18, 'olv_mid')
    p(qx, qy, 13, 18, 'olv_mid'); p(qx, qy, 14, 18, 'olv_shadow')
    p(qx, qy, 15, 18, 'strap_dark'); p(qx, qy, 16, 18, 'strap_dark')
    p(qx, qy, 17, 18, 'olv_mid'); p(qx, qy, 18, 18, 'olv_shadow')
    p(qx, qy, 19, 18, 'camo_dark'); p(qx, qy, 20, 18, 'olv_dark')
    p(qx, qy, 21, 18, 'olv_dark')

    # Outlines for flanks
    p(qx, qy, 9, 16, 'outline'); p(qx, qy, 9, 17, 'outline'); p(qx, qy, 9, 18, 'outline')
    p(qx, qy, 22, 16, 'outline'); p(qx, qy, 22, 17, 'outline'); p(qx, qy, 22, 18, 'outline')

    # Waist Belt & Buckle (y = 19..20)
    # y = 19
    p(qx, qy, 10, 19, 'outline')
    for x in range(11, 15): p(qx, qy, x, 19, 'strap_hi')
    p(qx, qy, 15, 19, 'metal_spec'); p(qx, qy, 16, 19, 'metal_hi')
    for x in range(17, 21): p(qx, qy, x, 19, 'strap_mid')
    p(qx, qy, 21, 19, 'outline')

    # y = 20
    p(qx, qy, 11, 20, 'outline')
    for x in range(12, 15): p(qx, qy, x, 20, 'strap_dark')
    p(qx, qy, 15, 20, 'metal_mid'); p(qx, qy, 16, 20, 'metal_dark')
    for x in range(17, 20): p(qx, qy, x, 20, 'strap_dark')
    p(qx, qy, 20, 20, 'outline')

    # Bottom outline
    for x in range(12, 20): p(qx, qy, x, 21, 'outline')

    # --- QUADRANT 2: EAST (Facing East / Right, 32, 0) ---
    qx, qy = 32, 0
    # Shoulder strap curving over shoulder (y = 10..12)
    p(qx, qy, 12, 10, 'outline'); p(qx, qy, 13, 10, 'olv_hi'); p(qx, qy, 14, 10, 'olv_light'); p(qx, qy, 15, 10, 'outline')
    p(qx, qy, 11, 11, 'outline'); p(qx, qy, 12, 11, 'metal_spec'); p(qx, qy, 13, 11, 'strap_mid'); p(qx, qy, 14, 11, 'strap_dark'); p(qx, qy, 15, 11, 'outline')
    p(qx, qy, 11, 12, 'outline'); p(qx, qy, 12, 12, 'olv_light'); p(qx, qy, 13, 12, 'camo_light'); p(qx, qy, 14, 12, 'strap_mid'); p(qx, qy, 15, 12, 'outline')

    # Side of torso & adjustment lacing (y = 13..15)
    p(qx, qy, 11, 13, 'outline'); p(qx, qy, 12, 13, 'strap_mid'); p(qx, qy, 13, 13, 'strap_dark'); p(qx, qy, 14, 13, 'strap_dark'); p(qx, qy, 15, 13, 'outline')
    p(qx, qy, 11, 14, 'outline'); p(qx, qy, 12, 14, 'metal_hi'); p(qx, qy, 13, 14, 'strap_mid'); p(qx, qy, 14, 14, 'metal_mid'); p(qx, qy, 15, 14, 'outline')

    # Side profile of Front Pouches (protruding forward at x = 16..20, y = 15..18)
    p(qx, qy, 15, 15, 'outline'); p(qx, qy, 16, 15, 'olv_hi'); p(qx, qy, 17, 15, 'metal_spec'); p(qx, qy, 18, 15, 'outline')
    p(qx, qy, 15, 16, 'olv_light'); p(qx, qy, 16, 16, 'camo_light'); p(qx, qy, 17, 16, 'fastex_dark'); p(qx, qy, 18, 16, 'outline')
    p(qx, qy, 15, 17, 'olv_mid'); p(qx, qy, 16, 17, 'camo_mid'); p(qx, qy, 17, 17, 'olv_shadow'); p(qx, qy, 18, 17, 'outline')
    p(qx, qy, 15, 18, 'olv_shadow'); p(qx, qy, 16, 18, 'olv_dark'); p(qx, qy, 17, 18, 'outline')

    # Side flank & Grenade Pouch (x = 11..14, y = 15..18)
    p(qx, qy, 11, 15, 'outline'); p(qx, qy, 12, 15, 'metal_spec'); p(qx, qy, 13, 15, 'olv_light'); p(qx, qy, 14, 15, 'camo_light')
    p(qx, qy, 11, 16, 'outline'); p(qx, qy, 12, 16, 'fastex_mid'); p(qx, qy, 13, 16, 'olv_mid'); p(qx, qy, 14, 16, 'camo_mid')
    p(qx, qy, 11, 17, 'outline'); p(qx, qy, 12, 17, 'olv_mid'); p(qx, qy, 13, 17, 'olv_shadow'); p(qx, qy, 14, 17, 'olv_dark')
    p(qx, qy, 11, 18, 'outline'); p(qx, qy, 12, 18, 'olv_shadow'); p(qx, qy, 13, 18, 'olv_dark'); p(qx, qy, 14, 18, 'outline')

    # Waist Belt side profile (y = 19..20)
    p(qx, qy, 11, 19, 'outline'); p(qx, qy, 12, 19, 'strap_hi'); p(qx, qy, 13, 19, 'strap_mid'); p(qx, qy, 14, 19, 'strap_mid'); p(qx, qy, 15, 19, 'strap_dark'); p(qx, qy, 16, 19, 'outline')
    p(qx, qy, 12, 20, 'outline'); p(qx, qy, 13, 20, 'outline'); p(qx, qy, 14, 20, 'outline'); p(qx, qy, 15, 20, 'outline')

    # --- QUADRANT 3: NORTH (Back View, 0, 32) ---
    qx, qy = 0, 32
    # Padded shoulder straps over upper back (y = 10..12)
    p(qx, qy, 11, 10, 'outline'); p(qx, qy, 12, 10, 'olv_light'); p(qx, qy, 13, 10, 'outline')
    p(qx, qy, 18, 10, 'outline'); p(qx, qy, 19, 10, 'olv_mid'); p(qx, qy, 20, 10, 'outline')

    p(qx, qy, 11, 11, 'outline'); p(qx, qy, 12, 11, 'olv_light'); p(qx, qy, 13, 11, 'outline')
    p(qx, qy, 18, 11, 'outline'); p(qx, qy, 19, 11, 'olv_shadow'); p(qx, qy, 20, 11, 'outline')

    # H-Harness crossing and horizontal back strap (y = 12..14)
    # y = 12
    p(qx, qy, 11, 12, 'outline'); p(qx, qy, 12, 12, 'camo_light'); p(qx, qy, 13, 12, 'strap_hi')
    for x in range(14, 18): p(qx, qy, x, 12, 'strap_mid')
    p(qx, qy, 18, 12, 'strap_dark'); p(qx, qy, 19, 12, 'olv_shadow'); p(qx, qy, 20, 12, 'outline')

    # y = 13 (Back central panel with metal adjustment sliders)
    p(qx, qy, 11, 13, 'outline'); p(qx, qy, 12, 13, 'strap_mid'); p(qx, qy, 13, 13, 'metal_spec')
    p(qx, qy, 14, 13, 'olv_light'); p(qx, qy, 15, 13, 'camo_light'); p(qx, qy, 16, 13, 'camo_mid'); p(qx, qy, 17, 13, 'olv_mid')
    p(qx, qy, 18, 13, 'metal_hi'); p(qx, qy, 19, 13, 'strap_dark'); p(qx, qy, 20, 13, 'outline')

    # y = 14 (Mesh back cushion / lower back panel)
    p(qx, qy, 11, 14, 'outline'); p(qx, qy, 12, 14, 'strap_dark'); p(qx, qy, 13, 14, 'outline')
    p(qx, qy, 14, 14, 'olv_light'); p(qx, qy, 15, 14, 'olv_mid'); p(qx, qy, 16, 14, 'olv_shadow'); p(qx, qy, 17, 14, 'camo_dark')
    p(qx, qy, 18, 14, 'outline'); p(qx, qy, 19, 14, 'strap_dark'); p(qx, qy, 20, 14, 'outline')

    # y = 15..17 (Back panel and adjustment straps)
    for y in [15, 16, 17]:
        p(qx, qy, 12, y, 'outline')
        p(qx, qy, 13, y, 'strap_mid' if y != 16 else 'metal_hi')
        p(qx, qy, 14, y, 'olv_light' if y == 15 else 'camo_mid')
        p(qx, qy, 15, y, 'camo_light' if y == 15 else 'olv_mid')
        p(qx, qy, 16, y, 'olv_mid' if y == 15 else 'olv_shadow')
        p(qx, qy, 17, y, 'olv_shadow' if y == 15 else 'camo_dark')
        p(qx, qy, 18, y, 'strap_dark' if y != 16 else 'metal_hi')
        p(qx, qy, 19, y, 'outline')

    # y = 18 (Lower back suspension rings)
    p(qx, qy, 12, 18, 'outline')
    p(qx, qy, 13, 18, 'metal_spec'); p(qx, qy, 14, 18, 'strap_mid')
    p(qx, qy, 15, 18, 'olv_shadow'); p(qx, qy, 16, 18, 'olv_dark')
    p(qx, qy, 17, 18, 'strap_dark'); p(qx, qy, 18, 18, 'metal_spec')
    p(qx, qy, 19, 18, 'outline')

    # Waist belt on back (y = 19..20)
    p(qx, qy, 10, 19, 'outline')
    for x in range(11, 21): p(qx, qy, x, 19, 'strap_mid' if x in [13, 18] else 'strap_dark')
    p(qx, qy, 21, 19, 'outline')

    p(qx, qy, 11, 20, 'outline')
    for x in range(12, 20): p(qx, qy, x, 20, 'strap_dark')
    p(qx, qy, 20, 20, 'outline')

    for x in range(12, 20): p(qx, qy, x, 21, 'outline')

    # --- QUADRANT 4: WEST (Facing West / Left, 32, 32) ---
    qx, qy = 32, 32
    # Mirrored profile of East (facing left)
    # Shoulder strap (y = 10..12)
    p(qx, qy, 16, 10, 'outline'); p(qx, qy, 17, 10, 'olv_light'); p(qx, qy, 18, 10, 'olv_hi'); p(qx, qy, 19, 10, 'outline')
    p(qx, qy, 16, 11, 'outline'); p(qx, qy, 17, 11, 'strap_dark'); p(qx, qy, 18, 11, 'strap_mid'); p(qx, qy, 19, 11, 'metal_spec'); p(qx, qy, 20, 11, 'outline')
    p(qx, qy, 16, 12, 'outline'); p(qx, qy, 17, 12, 'strap_mid'); p(qx, qy, 18, 12, 'camo_light'); p(qx, qy, 19, 12, 'olv_light'); p(qx, qy, 20, 12, 'outline')

    # Side of torso & adjustment lacing (y = 13..15)
    p(qx, qy, 16, 13, 'outline'); p(qx, qy, 17, 13, 'strap_dark'); p(qx, qy, 18, 13, 'strap_dark'); p(qx, qy, 19, 13, 'strap_mid'); p(qx, qy, 20, 13, 'outline')
    p(qx, qy, 16, 14, 'outline'); p(qx, qy, 17, 14, 'metal_mid'); p(qx, qy, 18, 14, 'strap_mid'); p(qx, qy, 19, 14, 'metal_hi'); p(qx, qy, 20, 14, 'outline')

    # Front Pouches protruding forward to the left (x = 13..16, y = 15..18)
    p(qx, qy, 13, 15, 'outline'); p(qx, qy, 14, 15, 'metal_spec'); p(qx, qy, 15, 15, 'olv_hi'); p(qx, qy, 16, 15, 'outline')
    p(qx, qy, 13, 16, 'outline'); p(qx, qy, 14, 16, 'fastex_dark'); p(qx, qy, 15, 16, 'camo_light'); p(qx, qy, 16, 16, 'olv_light')
    p(qx, qy, 13, 17, 'outline'); p(qx, qy, 14, 17, 'olv_shadow'); p(qx, qy, 15, 17, 'camo_mid'); p(qx, qy, 16, 17, 'olv_mid')
    p(qx, qy, 14, 18, 'outline'); p(qx, qy, 15, 18, 'olv_dark'); p(qx, qy, 16, 18, 'olv_shadow')

    # Side flank & Grenade Pouch (x = 17..20, y = 15..18)
    p(qx, qy, 17, 15, 'camo_light'); p(qx, qy, 18, 15, 'olv_light'); p(qx, qy, 19, 15, 'metal_spec'); p(qx, qy, 20, 15, 'outline')
    p(qx, qy, 17, 16, 'camo_mid'); p(qx, qy, 18, 16, 'olv_mid'); p(qx, qy, 19, 16, 'fastex_mid'); p(qx, qy, 20, 16, 'outline')
    p(qx, qy, 17, 17, 'olv_dark'); p(qx, qy, 18, 17, 'olv_shadow'); p(qx, qy, 19, 17, 'olv_mid'); p(qx, qy, 20, 17, 'outline')
    p(qx, qy, 17, 18, 'outline'); p(qx, qy, 18, 18, 'olv_dark'); p(qx, qy, 19, 18, 'olv_shadow'); p(qx, qy, 20, 18, 'outline')

    # Waist Belt side profile (y = 19..20)
    p(qx, qy, 15, 19, 'outline'); p(qx, qy, 16, 19, 'strap_dark'); p(qx, qy, 17, 19, 'strap_mid'); p(qx, qy, 18, 19, 'strap_mid'); p(qx, qy, 19, 19, 'strap_hi'); p(qx, qy, 20, 19, 'outline')
    p(qx, qy, 16, 20, 'outline'); p(qx, qy, 17, 20, 'outline'); p(qx, qy, 18, 20, 'outline'); p(qx, qy, 19, 20, 'outline')

    return Image.fromarray(canvas, 'RGBA')

# ========================================================
# 3. INHAND SPRITES (64x64, 4 directions)
# ========================================================
def create_inhand(is_right=False):
    canvas = np.zeros((64, 64, 4), dtype=np.uint8)

    def p(qx, qy, x, y, color):
        px = qx + x
        py = qy + y
        if 0 <= px < 64 and 0 <= py < 64:
            canvas[py, px] = PAL[color]

    # Hand offsets:
    # inhand-left: South=(19, 20), East=(5, 20), North=(18, 20), West=(13, 19)
    # inhand-right: South=(5, 20), East=(19, 20), North=(10, 19), West=(9, 20)
    positions = [
        (0, 0, (5 if is_right else 19), 20),      # South
        (32, 0, (19 if is_right else 5), 20),     # East
        (0, 32, (10 if is_right else 18), 20),    # North
        (32, 32, (9 if is_right else 13), 20),    # West
    ]

    for qx, qy, hx, hy in positions:
        # Draw folded/draped vest in hand
        # Top grab handle / shoulder strap loop in hand
        p(qx, qy, hx + 1, hy, 'outline'); p(qx, qy, hx + 2, hy, 'strap_mid'); p(qx, qy, hx + 3, hy, 'outline')
        p(qx, qy, hx, hy + 1, 'outline'); p(qx, qy, hx + 1, hy + 1, 'metal_spec'); p(qx, qy, hx + 2, hy + 1, 'strap_hi'); p(qx, qy, hx + 3, hy + 1, 'metal_hi'); p(qx, qy, hx + 4, hy + 1, 'outline')
        
        # Draped body of vest with pouches hanging down
        for dy in range(2, 8):
            y = hy + dy
            p(qx, qy, hx, y, 'outline')
            p(qx, qy, hx + 1, y, 'olv_hi' if dy in [2, 5] else 'camo_light')
            p(qx, qy, hx + 2, y, 'olv_light' if dy in [3, 6] else 'camo_mid')
            p(qx, qy, hx + 3, y, 'fastex_dark' if dy == 3 else 'olv_mid')
            p(qx, qy, hx + 4, y, 'olv_shadow' if dy in [4, 7] else 'camo_dark')
            p(qx, qy, hx + 5, y, 'outline')
        
        # Bottom trim of folded vest
        p(qx, qy, hx + 1, hy + 8, 'outline'); p(qx, qy, hx + 2, hy + 8, 'strap_dark'); p(qx, qy, hx + 3, hy + 8, 'metal_mid'); p(qx, qy, hx + 4, hy + 8, 'outline')
        p(qx, qy, hx + 2, hy + 9, 'outline'); p(qx, qy, hx + 3, hy + 9, 'outline')

    return Image.fromarray(canvas, 'RGBA')

# Generate all
icon_img = create_icon()
equipped_img = create_equipped_belt()
inhand_l_img = create_inhand(is_right=False)
inhand_r_img = create_inhand(is_right=True)

# Save test files
icon_img.save('scratch/6sh92_icon.png')
equipped_img.save('scratch/6sh92_equipped.png')
inhand_l_img.save('scratch/6sh92_inhand_left.png')
inhand_r_img.save('scratch/6sh92_inhand_right.png')
print('All 6Sh92 sprites rendered successfully!')
