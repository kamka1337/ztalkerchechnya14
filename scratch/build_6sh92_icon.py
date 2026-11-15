import numpy as np
from PIL import Image

def hex_to_rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# Server-matching military palette
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

def create_icon():
    canvas = np.zeros((32, 32, 4), dtype=np.uint8)

    def p(x, y, color):
        if 0 <= x < 32 and 0 <= y < 32:
            canvas[y, x] = PAL[color]

    # --- 1. Shoulder Straps & Upper Yoke (y = 6..10) ---
    # Left strap (x = 9..12)
    # y = 6
    p(9, 6, 'outline'); p(10, 6, 'olv_hi'); p(11, 6, 'olv_light'); p(12, 6, 'outline')
    # y = 7
    p(9, 7, 'outline'); p(10, 7, 'olv_light'); p(11, 7, 'olv_mid'); p(12, 7, 'outline')
    # y = 8 (Metal D-Ring on left strap)
    p(8, 8, 'outline'); p(9, 8, 'metal_spec'); p(10, 8, 'metal_hi'); p(11, 8, 'strap_mid'); p(12, 8, 'outline')
    # y = 9
    p(8, 9, 'outline'); p(9, 9, 'olv_light'); p(10, 9, 'camo_light'); p(11, 9, 'olv_mid'); p(12, 9, 'outline')
    # y = 10
    p(8, 10, 'outline'); p(9, 10, 'olv_light'); p(10, 10, 'camo_mid'); p(11, 10, 'olv_mid'); p(12, 10, 'outline')

    # Right strap (x = 19..22)
    # y = 6
    p(19, 6, 'outline'); p(20, 6, 'olv_light'); p(21, 6, 'olv_mid'); p(22, 6, 'outline')
    # y = 7
    p(19, 7, 'outline'); p(20, 7, 'camo_light'); p(21, 7, 'camo_mid'); p(22, 7, 'outline')
    # y = 8 (Metal D-Ring on right strap)
    p(19, 8, 'outline'); p(20, 8, 'strap_mid'); p(21, 8, 'metal_hi'); p(22, 8, 'metal_spec'); p(23, 8, 'outline')
    # y = 9
    p(19, 9, 'outline'); p(20, 9, 'olv_light'); p(21, 9, 'olv_mid'); p(22, 9, 'olv_shadow'); p(23, 9, 'outline')
    # y = 10
    p(19, 10, 'outline'); p(20, 10, 'olv_light'); p(21, 10, 'olv_mid'); p(22, 10, 'olv_shadow'); p(23, 10, 'outline')

    # Back neck collar strip visible in V-neck (y = 7..9, x = 13..18)
    p(13, 7, 'outline'); p(14, 7, 'olv_dark'); p(15, 7, 'strap_dark'); p(16, 7, 'strap_dark'); p(17, 7, 'olv_dark'); p(18, 7, 'outline')
    p(14, 8, 'outline'); p(15, 8, 'dark_crevice'); p(16, 8, 'dark_crevice'); p(17, 8, 'outline')

    # --- 2. Upper Chest: Bayonet Pocket & Utility Pocket (y = 11..14) ---
    # Left Chest (x = 7..14): Bayonet/Knife sheath pocket & left chest panel
    # y = 11
    p(7, 11, 'outline')
    p(8, 11, 'olv_hi'); p(9, 11, 'metal_hi'); p(10, 11, 'metal_spec'); p(11, 11, 'fastex_mid'); p(12, 11, 'olv_light'); p(13, 11, 'olv_mid')
    p(14, 11, 'outline')

    # y = 12
    p(7, 12, 'outline')
    p(8, 12, 'olv_light'); p(9, 12, 'strap_hi'); p(10, 12, 'strap_mid'); p(11, 12, 'strap_dark'); p(12, 12, 'camo_light'); p(13, 12, 'camo_mid')
    p(14, 12, 'outline')

    # y = 13 (Bayonet sheath retention snap)
    p(7, 13, 'outline')
    p(8, 13, 'olv_light'); p(9, 13, 'metal_spec'); p(10, 13, 'metal_hi'); p(11, 13, 'strap_mid'); p(12, 13, 'camo_mid'); p(13, 13, 'olv_shadow')
    p(14, 13, 'outline')

    # y = 14
    p(7, 14, 'outline')
    p(8, 14, 'olv_light'); p(9, 14, 'strap_mid'); p(10, 14, 'strap_dark'); p(11, 14, 'olv_mid'); p(12, 14, 'olv_mid'); p(13, 14, 'olv_shadow')
    p(14, 14, 'outline')

    # Center Chest Closure: Upper Fastex Buckle (x = 14..17, y = 12..13)
    p(14, 12, 'outline'); p(15, 12, 'fastex_hi'); p(16, 12, 'fastex_mid'); p(17, 12, 'outline')
    p(14, 13, 'outline'); p(15, 13, 'fastex_mid'); p(16, 13, 'fastex_dark'); p(17, 13, 'outline')

    # Right Chest (x = 17..24): Utility / Bandage (IPP) Pocket
    # y = 11
    p(17, 11, 'outline')
    p(18, 11, 'olv_light'); p(19, 11, 'camo_light'); p(20, 11, 'camo_mid'); p(21, 11, 'olv_mid'); p(22, 11, 'olv_shadow'); p(23, 11, 'olv_dark')
    p(24, 11, 'outline')

    # y = 12 (Utility pocket flap with snap)
    p(17, 12, 'outline')
    p(18, 12, 'olv_light'); p(19, 12, 'olv_light'); p(20, 12, 'metal_spec'); p(21, 12, 'metal_hi'); p(22, 12, 'olv_shadow'); p(23, 12, 'olv_dark')
    p(24, 12, 'outline')

    # y = 13
    p(17, 13, 'outline')
    p(18, 13, 'olv_light'); p(19, 13, 'olv_mid'); p(20, 13, 'fastex_mid'); p(21, 13, 'olv_mid'); p(22, 13, 'olv_shadow'); p(23, 13, 'olv_dark')
    p(24, 13, 'outline')

    # y = 14
    p(17, 14, 'outline')
    p(18, 14, 'camo_light'); p(19, 14, 'camo_mid'); p(20, 14, 'camo_dark'); p(21, 14, 'olv_shadow'); p(22, 14, 'olv_dark'); p(23, 14, 'olv_dark')
    p(24, 14, 'outline')

    # --- 3. AK Magazine Pouches & Grenade Pouches (y = 15..20) ---
    # In 6Sh92: 4 prominent AK mag pouches across front + 2 grenade pouches on flanks!
    # Left Grenade Pouch (x = 5..7, y = 16..20)
    # y = 16 (Grenade pouch flap with snap)
    p(5, 16, 'outline'); p(6, 16, 'olv_hi'); p(7, 16, 'metal_spec'); p(8, 16, 'outline')
    # y = 17
    p(5, 17, 'outline'); p(6, 17, 'olv_light'); p(7, 17, 'fastex_mid'); p(8, 17, 'outline')
    # y = 18
    p(5, 18, 'outline'); p(6, 18, 'olv_light'); p(7, 18, 'olv_mid'); p(8, 18, 'outline')
    # y = 19
    p(5, 19, 'outline'); p(6, 19, 'olv_mid'); p(7, 19, 'olv_shadow'); p(8, 19, 'outline')
    # y = 20
    p(6, 20, 'outline'); p(7, 20, 'olv_shadow'); p(8, 20, 'outline')

    # AK Mag Pouch 1 (x = 8..10) & Mag Pouch 2 (x = 11..13)
    # y = 15 (Flaps)
    p(8, 15, 'outline'); p(9, 15, 'olv_hi'); p(10, 15, 'olv_light') # Pouch 1 flap
    p(11, 15, 'outline'); p(12, 15, 'olv_hi'); p(13, 15, 'olv_light'); p(14, 15, 'outline') # Pouch 2 flap

    # y = 16 (Snap buttons & pull tabs)
    p(8, 16, 'outline'); p(9, 16, 'metal_spec'); p(10, 16, 'fastex_dark')
    p(11, 16, 'outline'); p(12, 16, 'metal_spec'); p(13, 16, 'fastex_dark'); p(14, 16, 'outline')

    # y = 17 (Pouch Bodies with Flora camo)
    p(8, 17, 'outline'); p(9, 17, 'olv_light'); p(10, 17, 'camo_light')
    p(11, 17, 'dark_crevice'); p(12, 17, 'olv_light'); p(13, 17, 'olv_mid'); p(14, 17, 'outline')

    # y = 18
    p(8, 18, 'outline'); p(9, 18, 'olv_light'); p(10, 18, 'camo_mid')
    p(11, 18, 'dark_crevice'); p(12, 18, 'camo_light'); p(13, 18, 'camo_mid'); p(14, 18, 'outline')

    # y = 19
    p(8, 19, 'outline'); p(9, 19, 'olv_mid'); p(10, 19, 'camo_dark')
    p(11, 19, 'dark_crevice'); p(12, 19, 'olv_mid'); p(13, 19, 'olv_shadow'); p(14, 19, 'outline')

    # y = 20
    p(8, 20, 'outline'); p(9, 20, 'olv_shadow'); p(10, 20, 'olv_shadow')
    p(11, 20, 'dark_crevice'); p(12, 20, 'olv_shadow'); p(13, 20, 'olv_dark'); p(14, 20, 'outline')

    # Center Lower Closure / Strap (x = 14..17, y = 16..18)
    p(14, 16, 'outline'); p(15, 16, 'fastex_hi'); p(16, 16, 'fastex_mid'); p(17, 16, 'outline')
    p(14, 17, 'outline'); p(15, 17, 'fastex_mid'); p(16, 17, 'fastex_dark'); p(17, 17, 'outline')
    p(14, 18, 'outline'); p(15, 18, 'strap_mid'); p(16, 18, 'strap_dark'); p(17, 18, 'outline')

    # AK Mag Pouch 3 (x = 17..19) & Mag Pouch 4 (x = 20..22)
    # y = 15 (Flaps)
    p(17, 15, 'outline'); p(18, 15, 'olv_light'); p(19, 15, 'olv_mid'); p(20, 15, 'outline') # Pouch 3
    p(21, 15, 'olv_light'); p(22, 15, 'olv_mid'); p(23, 15, 'outline') # Pouch 4

    # y = 16 (Snap buttons & pull tabs)
    p(17, 16, 'outline'); p(18, 16, 'metal_spec'); p(19, 16, 'fastex_dark'); p(20, 16, 'outline')
    p(21, 16, 'metal_spec'); p(22, 16, 'fastex_dark'); p(23, 16, 'outline')

    # y = 17 (Pouch bodies with Flora camo)
    p(17, 17, 'outline'); p(18, 17, 'camo_light'); p(19, 17, 'camo_mid'); p(20, 17, 'dark_crevice')
    p(21, 17, 'olv_light'); p(22, 17, 'olv_mid'); p(23, 17, 'outline')

    # y = 18
    p(17, 18, 'outline'); p(18, 18, 'camo_mid'); p(19, 18, 'camo_dark'); p(20, 18, 'dark_crevice')
    p(21, 18, 'olv_mid'); p(22, 18, 'olv_shadow'); p(23, 18, 'outline')

    # y = 19
    p(17, 19, 'outline'); p(18, 19, 'olv_mid'); p(19, 19, 'olv_shadow'); p(20, 19, 'dark_crevice')
    p(21, 19, 'olv_shadow'); p(22, 19, 'olv_dark'); p(23, 19, 'outline')

    # y = 20
    p(17, 20, 'outline'); p(18, 20, 'olv_shadow'); p(19, 20, 'olv_dark'); p(20, 20, 'dark_crevice')
    p(21, 20, 'olv_shadow'); p(22, 20, 'olv_dark'); p(23, 20, 'outline')

    # Right Grenade Pouch (x = 23..26, y = 16..20)
    # y = 16
    p(23, 16, 'outline'); p(24, 16, 'metal_spec'); p(25, 16, 'olv_mid'); p(26, 16, 'outline')
    # y = 17
    p(23, 17, 'outline'); p(24, 17, 'fastex_mid'); p(25, 17, 'olv_shadow'); p(26, 17, 'outline')
    # y = 18
    p(23, 18, 'outline'); p(24, 18, 'olv_mid'); p(25, 18, 'olv_shadow'); p(26, 18, 'outline')
    # y = 19
    p(23, 19, 'outline'); p(24, 19, 'olv_shadow'); p(25, 19, 'olv_dark'); p(26, 19, 'outline')
    # y = 20
    p(23, 20, 'outline'); p(24, 20, 'olv_dark'); p(25, 20, 'outline')

    # --- 4. Integrated Heavy Waist Belt (y = 21..24) ---
    # Belt outline top
    p(8, 21, 'outline')
    for x in range(9, 23):
        p(x, 21, 'strap_hi' if x in [10, 13, 18, 21] else 'strap_mid')
    p(23, 21, 'outline')

    # Belt body & Central Metal Frame Buckle (y = 22)
    p(8, 22, 'outline')
    for x in range(9, 14):
        p(x, 22, 'strap_mid')
    # Central steel buckle (x = 14..17)
    p(14, 22, 'metal_hi'); p(15, 22, 'metal_spec'); p(16, 22, 'metal_spec'); p(17, 22, 'metal_hi')
    for x in range(18, 23):
        p(x, 22, 'strap_dark')
    p(23, 22, 'outline')

    # Belt bottom line (y = 23)
    p(9, 23, 'outline')
    for x in range(10, 14):
        p(x, 23, 'strap_dark')
    p(14, 23, 'metal_mid'); p(15, 23, 'metal_hi'); p(16, 23, 'metal_hi'); p(17, 23, 'metal_mid')
    for x in range(18, 22):
        p(x, 23, 'strap_dark')
    p(22, 23, 'outline')

    # Bottom outline (y = 24)
    for x in range(9, 23):
        p(x, 24, 'outline')

    return Image.fromarray(canvas, 'RGBA')

img = create_icon()
img.save('scratch/6sh92_icon_test.png')
print('Icon test generated, bbox:', img.getbbox())
