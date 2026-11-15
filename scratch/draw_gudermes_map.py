import numpy as np
from PIL import Image, ImageDraw, ImageFont

def render_map():
    W, H = 1400, 950
    img = Image.new('RGB', (W, H), (28, 32, 28))
    draw = ImageDraw.Draw(img)

    # 1. Background terrain gradient / winter-mud ground
    for y in range(H):
        ratio = y / H
        # subtle tone variation
        r = int(32 + ratio * 8)
        g = int(38 + ratio * 6)
        b = int(34 + ratio * 4)
        draw.line([(0, y), (W, y)], fill=(r, g, b))

    # Grid overlay (100x100 grid cells)
    grid_size = 100
    for x in range(0, W, grid_size):
        draw.line([(x, 0), (x, H)], fill=(45, 52, 45, 255), width=1)
    for y in range(0, H, grid_size):
        draw.line([(0, y), (W, y)], fill=(45, 52, 45, 255), width=1)

    # River Gums (река Гумс / Белка) flowing on the east side
    river_pts = [
        (1050, 0), (1070, 150), (1110, 300), (1130, 450), 
        (1160, 600), (1200, 750), (1230, 950)
    ]
    # Draw wide river
    draw.line(river_pts, fill=(35, 55, 65), width=45)
    draw.line(river_pts, fill=(45, 75, 90), width=35)
    draw.line(river_pts, fill=(55, 95, 115), width=20)
    draw.line(river_pts, fill=(80, 125, 145), width=6)

    # Hills / Contours (Холмы на севере и юге)
    # North hills
    draw.polygon([(0, 0), (450, 0), (350, 120), (120, 140), (0, 80)], fill=(38, 46, 36))
    draw.polygon([(0, 0), (300, 0), (180, 80), (0, 50)], fill=(48, 58, 45))
    # South hills
    draw.polygon([(600, 950), (1400, 950), (1400, 800), (950, 820), (700, 890)], fill=(38, 46, 36))

    # Roads network
    # Main highway M29 (horizontal artery across map)
    road_main = [(50, 520), (350, 510), (680, 510), (950, 520), (1140, 520), (1350, 530)]
    draw.line(road_main, fill=(65, 65, 62), width=28)
    draw.line(road_main, fill=(85, 85, 80), width=22)
    # Center dashed road line
    draw.line(road_main, fill=(160, 160, 150), width=2)

    # North-South secondary roads
    draw.line([(380, 50), (380, 900)], fill=(55, 55, 50), width=16)
    draw.line([(680, 50), (680, 900)], fill=(55, 55, 50), width=16)
    draw.line([(950, 80), (950, 850)], fill=(55, 55, 50), width=14)

    # Diagonal connector streets
    draw.line([(380, 280), (680, 510)], fill=(50, 50, 48), width=12)
    draw.line([(680, 510), (950, 750)], fill=(50, 50, 48), width=12)
    draw.line([(380, 700), (680, 510)], fill=(50, 50, 48), width=12)

    # Railway tracks (Multi-track railway junction cutting through north-central)
    rail_pts1 = [(50, 360), (380, 370), (700, 380), (1050, 360), (1350, 350)]
    rail_pts2 = [(50, 375), (380, 385), (700, 395), (1050, 375), (1350, 365)]
    rail_pts3 = [(50, 390), (380, 400), (700, 410), (1050, 390), (1350, 380)]
    rail_pts4 = [(380, 415), (700, 425), (950, 410)] # siding track

    # Ballast gravel bed
    draw.line([(50, 375), (1350, 365)], fill=(45, 45, 48), width=50)

    for r_pts in [rail_pts1, rail_pts2, rail_pts3, rail_pts4]:
        draw.line(r_pts, fill=(120, 125, 130), width=4)
        draw.line(r_pts, fill=(170, 175, 180), width=2)

    # Train cars / static rolling stock on tracks (brown freight boxes)
    cars = [
        (480, 365, 560, 375), (580, 365, 650, 375),
        (760, 390, 840, 400), (860, 390, 920, 400),
        (520, 410, 600, 420),
    ]
    for c in cars:
        draw.rectangle(c, fill=(95, 65, 45), outline=(30, 20, 15), width=2)

    # City blocks & buildings (Частный сектор и городская застройка)
    # Residential blocks (grey/brick squares)
    res_blocks = [
        # West blocks (around hospital)
        (220, 580, 320, 650), (220, 680, 320, 750), (150, 600, 200, 720),
        # North blocks (around elevator)
        (460, 140, 560, 220), (580, 140, 660, 220),
        # Central-East blocks
        (760, 580, 860, 650), (880, 580, 930, 650),
        # South blocks
        (460, 750, 560, 830), (580, 750, 660, 830),
    ]
    for b in res_blocks:
        draw.rectangle(b, fill=(45, 48, 45), outline=(60, 65, 60), width=1)
        # Small house footprints inside
        for hx in range(b[0]+8, b[2]-15, 30):
            for hy in range(b[1]+8, b[3]-15, 25):
                draw.rectangle((hx, hy, hx+18, hy+16), fill=(65, 62, 58), outline=(30, 30, 28))

    # River Bridge (Мост через реку Гумс)
    bridge_box = (1120, 508, 1155, 532)
    draw.rectangle(bridge_box, fill=(90, 92, 95), outline=(30, 30, 30), width=2)
    # Bridge piers / barrier
    draw.line([(1120, 505), (1155, 505)], fill=(180, 180, 180), width=3)
    draw.line([(1120, 535), (1155, 535)], fill=(180, 180, 180), width=3)

    # ====================================================
    # 2. BASES (ГЛАВНЫЕ СПАВНЫ КОМАНД)
    # ====================================================
    # BASE 1: БАЗА ВС РФ (North-West Sector: Военный городок / Блокпост)
    rf_base_rect = (70, 90, 280, 260)
    draw.rectangle(rf_base_rect, fill=(35, 48, 65), outline=(60, 120, 210), width=3)
    # Sandbag & barricade perimeter
    draw.rectangle((80, 100, 270, 250), fill=(28, 36, 48), outline=(80, 140, 220), width=1)
    # Barracks & HQ inside
    draw.rectangle((100, 120, 170, 170), fill=(50, 65, 85), outline=(20, 30, 45), width=2)
    draw.rectangle((190, 120, 250, 170), fill=(50, 65, 85), outline=(20, 30, 45), width=2)
    draw.rectangle((120, 190, 230, 235), fill=(40, 55, 75), outline=(20, 30, 45), width=2)
    # Flag marker
    draw.ellipse((160, 135, 190, 165), fill=(0, 100, 220), outline=(255, 255, 255), width=2)

    # BASE 2: БАЗА ЧРИ (South-East Sector: Зелёная зона / Опорный пункт за рекой)
    cri_base_rect = (1130, 680, 1340, 890)
    draw.rectangle(cri_base_rect, fill=(45, 58, 40), outline=(60, 180, 90), width=3)
    draw.rectangle((1140, 690, 1330, 880), fill=(32, 44, 28), outline=(80, 200, 110), width=1)
    # Fortified compound inside
    draw.rectangle((1160, 710, 1230, 770), fill=(55, 75, 50), outline=(25, 40, 20), width=2)
    draw.rectangle((1250, 710, 1310, 770), fill=(55, 75, 50), outline=(25, 40, 20), width=2)
    draw.rectangle((1180, 795, 1290, 860), fill=(50, 70, 45), outline=(25, 40, 20), width=2)
    # Flag marker
    draw.ellipse((1220, 725, 1250, 755), fill=(20, 160, 60), outline=(255, 255, 255), width=2)

    # ====================================================
    # 3. CAPTURE POINTS (ТОЧКИ ЗАХВАТА / ПЕРЕДОВЫЕ РЕСПАВНЫ)
    # ====================================================
    points = [
        {
            'id': 'B',
            'name': 'ТОЧКА [B]: Ж/Д ВОКЗАЛ',
            'sub': 'Центральный узел / Перроны и станция',
            'pos': (680, 450),
            'radius': 60,
            'building': (620, 430, 740, 470),
            'type': 'central'
        },
        {
            'id': 'A',
            'name': 'ТОЧКА [A]: ЭЛЕВАТОР',
            'sub': 'Северная промзона / Высотные башни',
            'pos': (720, 190),
            'radius': 50,
            'building': (680, 160, 760, 220),
            'type': 'industrial'
        },
        {
            'id': 'C',
            'name': 'ТОЧКА [C]: БОЛЬНИЦА',
            'sub': 'Западный сектор / 2-эт. комплекс',
            'pos': (280, 460),
            'radius': 50,
            'building': (240, 430, 320, 490),
            'type': 'urban'
        },
        {
            'id': 'D',
            'name': 'ТОЧКА [D]: РЫНОК И АВТОВОКЗАЛ',
            'sub': 'Южный сектор / Торговые ряды',
            'pos': (680, 720),
            'radius': 55,
            'building': (630, 690, 730, 750),
            'type': 'market'
        },
        {
            'id': 'E',
            'name': 'ТОЧКА [E]: МОСТ ЧЕРЕЗ ГУМС',
            'sub': 'Восточная переправа / Речной рубеж',
            'pos': (1135, 520),
            'radius': 45,
            'building': (1115, 495, 1160, 545),
            'type': 'bridge'
        }
    ]

    for pt in points:
        px, py = pt['pos']
        r = pt['radius']
        
        # Zone boundary circle (capture capture zone)
        draw.ellipse((px - r, py - r, px + r, py + r), fill=(255, 210, 40, 40), outline=(255, 205, 30), width=2)
        # Dash outer ring
        draw.ellipse((px - r - 6, py - r - 6, px + r + 6, py + r + 6), outline=(220, 180, 20), width=1)

        # Structure inside point
        bx = pt['building']
        draw.rectangle(bx, fill=(75, 78, 80), outline=(140, 145, 150), width=2)

        # Capture flag icon badge (Center)
        draw.ellipse((px - 18, py - 18, px + 18, py + 18), fill=(235, 175, 20), outline=(255, 255, 255), width=2)
        # Letter
        draw.text((px - 6, py - 9), pt['id'], fill=(10, 10, 10))

    # ====================================================
    # 4. TACTICAL VECTORS & ATTACK ROUTES (Стрелки наступления)
    # ====================================================
    # RF Vectors (Blue arrows)
    draw.line([(280, 180), (620, 190)], fill=(80, 160, 255), width=4) # to Elevator
    draw.polygon([(630, 190), (615, 180), (615, 200)], fill=(80, 160, 255))

    draw.line([(200, 260), (260, 400)], fill=(80, 160, 255), width=4) # to Hospital
    draw.polygon([(265, 410), (255, 395), (275, 400)], fill=(80, 160, 255))

    draw.line([(280, 220), (620, 420)], fill=(80, 160, 255), width=4) # to Station
    draw.polygon([(630, 425), (615, 415), (625, 435)], fill=(80, 160, 255))

    # CRI Vectors (Green arrows)
    draw.line([(1130, 750), (750, 730)], fill=(80, 230, 120), width=4) # to Market
    draw.polygon([(740, 730), (755, 720), (755, 740)], fill=(80, 230, 120))

    draw.line([(1180, 680), (1145, 575)], fill=(80, 230, 120), width=4) # to Bridge
    draw.polygon([(1140, 565), (1135, 580), (1155, 580)], fill=(80, 230, 120))

    draw.line([(1130, 710), (750, 480)], fill=(80, 230, 120), width=4) # to Station
    draw.polygon([(740, 475), (755, 465), (750, 485)], fill=(80, 230, 120))

    # ====================================================
    # 5. LABELS & HUD TEXT
    # ====================================================
    # Title Header Block
    draw.rectangle((20, 20, 520, 75), fill=(18, 22, 20), outline=(70, 90, 75), width=2)
    draw.text((35, 28), "ТАКТИЧЕСКАЯ КАРТА: «ГУДЕРМЕС — ДЕКАБРЬ 1995»", fill=(240, 245, 235))
    draw.text((35, 48), "Схема секторов, опорных пунктов и передовых респавнов (FOB)", fill=(160, 185, 165))

    # Base Labels
    draw.rectangle((70, 65, 230, 90), fill=(20, 30, 45), outline=(60, 120, 210), width=1)
    draw.text((78, 70), "БАЗА ВС РФ (СЕВЕР)", fill=(120, 180, 255))

    draw.rectangle((1180, 890, 1340, 915), fill=(25, 40, 25), outline=(60, 180, 90), width=1)
    draw.text((1188, 895), "БАЗА ЧРИ (ЮГО-ВОСТОК)", fill=(120, 235, 140))

    # Capture Point Text Labels with backgrounds
    for pt in points:
        px, py = pt['pos']
        # Box above point
        box_w = 260
        box_h = 38
        bx0 = px - box_w // 2
        by0 = py - pt['radius'] - 48
        
        # Adjust if too close to edges
        if bx0 < 20: bx0 = 20
        if bx0 + box_w > W - 20: bx0 = W - box_w - 20
        if by0 < 80: by0 = py + pt['radius'] + 10

        draw.rectangle((bx0, by0, bx0 + box_w, by0 + box_h), fill=(18, 20, 22), outline=(235, 175, 20), width=1)
        draw.text((bx0 + 10, by0 + 5), pt['name'], fill=(255, 225, 80))
        draw.text((bx0 + 10, by0 + 20), pt['sub'], fill=(180, 190, 185))

    # Compass Rose / North Indicator
    cx, cy = 1330, 80
    draw.polygon([(cx, cy - 35), (cx - 12, cy + 10), (cx, cy + 5)], fill=(220, 40, 40))
    draw.polygon([(cx, cy - 35), (cx + 12, cy + 10), (cx, cy + 5)], fill=(180, 30, 30))
    draw.polygon([(cx, cy + 35), (cx - 12, cy + 10), (cx, cy + 5)], fill=(180, 180, 180))
    draw.polygon([(cx, cy + 35), (cx + 12, cy + 10), (cx, cy + 5)], fill=(120, 120, 120))
    draw.text((cx - 4, cy - 50), "С", fill=(255, 255, 255))
    draw.text((cx - 4, cy + 38), "Ю", fill=(200, 200, 200))

    # Tactical Info Panel (Bottom Left HUD)
    hud_x, hud_y = 20, 770
    draw.rectangle((hud_x, hud_y, hud_x + 420, hud_y + 160), fill=(16, 20, 18), outline=(60, 80, 65), width=2)
    draw.text((hud_x + 15, hud_y + 12), "МЕХАНИКА ЗАХВАТА И РЕСПАВНОВ:", fill=(255, 215, 60))
    draw.text((hud_x + 15, hud_y + 35), "1. Базовые респавны: постоянные (нельзя захватить)", fill=(210, 220, 215))
    draw.text((hud_x + 15, hud_y + 55), "2. Точки A, B, C, D, E: становятся активными респавнами", fill=(210, 220, 215))
    draw.text((hud_x + 15, hud_y + 73), "   для той фракции, которая удерживает точку.", fill=(160, 175, 165))
    draw.text((hud_x + 15, hud_y + 95), "3. Время захвата: 45 сек при нахождении в радиусе круга.", fill=(210, 220, 215))
    draw.text((hud_x + 15, hud_y + 115), "4. Точка B (Вокзал) открывает прямой доступ ко всем флангам.", fill=(210, 220, 215))
    draw.text((hud_x + 15, hud_y + 135), "5. Точка A (Элеватор) даёт контроль над ж/д путями.", fill=(210, 220, 215))

    return img

map_img = render_map()
artifact_dir = 'C:/Users/kamka1337/.gemini/antigravity/brain/6dcd7435-10d7-47f9-9553-91e3e5314e42'
map_img.save(f'{artifact_dir}/gudermes_tactical_map.png')
print('Tactical map rendered and saved successfully!')
