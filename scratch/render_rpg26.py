import numpy as np
from PIL import Image

def hex_to_rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))

def hex_to_rgba(h, a=255):
    return (*hex_to_rgb(h), a)

# Palette definition
C = {
    # Tube olive drab ramp (6 rich shades)
    'tube_spec':      hex_to_rgba('a2b662'), # Specular glint
    'tube_hi':        hex_to_rgba('889b4f'), # Cylinder highlight
    'tube_mid_hi':    hex_to_rgba('6c7f3b'), # Upper body
    'tube_mid':       hex_to_rgba('53632b'), # Core base color
    'tube_shadow':    hex_to_rgba('3a461c'), # Lower body shadow
    'tube_dark':      hex_to_rgba('242d10'), # Deep ambient shade
    'tube_outline':   hex_to_rgba('121707'), # Darkest outline

    # Black / Dark steel parts (rubber bumpers, sight mounts, sights)
    'metal_hi':       hex_to_rgba('5c626c'), # Steel highlight
    'metal_mid_hi':   hex_to_rgba('42464e'), # Metal upper
    'metal_mid':      hex_to_rgba('2a2d33'), # Metal core
    'metal_shadow':   hex_to_rgba('1b1d22'), # Dark rubber / shadow
    'metal_outline':  hex_to_rgba('0e0f13'), # Purest dark edge

    # Mechanical steel rod (тяга)
    'rod_hi':         hex_to_rgba('8a9388'),
    'rod_mid':        hex_to_rgba('5e665c'),
    'rod_shadow':     hex_to_rgba('3c423b'),

    # Placard (инструкция)
    'placard_bg':     hex_to_rgba('181b1f'),
    'placard_txt':    hex_to_rgba('dee3de'),
    'placard_dim':    hex_to_rgba('8e968f'),

    # Safety red accent
    'red_hi':         hex_to_rgba('e83c35'),
    'red_mid':        hex_to_rgba('b8251f'),
    'red_shadow':     hex_to_rgba('6e130f'),

    # Nozzle / bore dark interior
    'bore_deep':      hex_to_rgba('0a0b0d'),
}

img = np.zeros((48, 48, 4), dtype=np.uint8)

def put(x, y, color):
    if 0 <= x < 48 and 0 <= y < 48:
        img[y, x] = color

# Let's define the tube centerline and build the cylinder
# Center axis goes from BL (bottom-left) to TR (top-right)
# Let's trace steps from (9, 39) to (38, 10): 30 diagonal steps
# Vector: dx = +1, dy = -1
# Perpendicular vector to top-left (upwards/light): (-1, 0) and (0, -1) -> (-1, -1)
# Perpendicular vector to bottom-right (downwards/shadow): (+1, 0) and (0, +1) -> (+1, +1)

# Let's generate base coordinates along the tube axis
# step t goes from 0 to 30:
# axis(t) = (9 + t, 39 - t)
# Thickness across: 
# Layer -2: Outline top-left
# Layer -1: Highlight strip (top surface)
# Layer  0: Mid-high / core (upper center)
# Layer +1: Core / shadow (lower center)
# Layer +2: Lower shadow
# Layer +3: Underside outline

# Let's write out the drawing logic
with open('scratch/draw_test.py', 'w') as f:
    f.write('# test')
print('Helper ready')
