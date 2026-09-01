# Tiny Monster Keeper - Ke hoach nang cap gameplay it ton asset

Ngay nghien cuu: 2026-08-31

## 1. Muc tieu

Tai lieu nay de xuat cac nang cap gameplay voi nhung gioi han sau:

- giu nguyen ban sac game suu tap pixel cozy;
- tai su dung monster, item, zone, animation, bubble va phong cach UI hien co;
- tranh mo rong sang combat, trang bi, xay dung hoac trang tri quy mo lon;
- cai thien lan choi dau, lua chon co y nghia, gia tri suu tap va muc tieu dai han;
- kiem chung gia dinh bang funnel va du lieu retention tu nguoi choi that.

Vong lap hien tai:

`thu hoach -> nau an -> trieu hoi -> cham soc/nhan coin -> mo zone`

Diem thu hut manh nhat la 27 monster, khu vuon co su song, kham pha cong thuc va
mo rong ban do. Diem yeu chinh la muc tieu chua ro, thoi gian cho thu dong nhieu,
cong thuc de thanh doan mo, monster chua co nhieu cong dung va khong co muc tieu
ro rang sau Zone13.

## 2. Nguyen tac nghien cuu

### Onboarding ro rang

Mo hinh A-G-E cua GDC chia onboarding thanh Attraction (thu hut), Goal (muc tieu)
va Effectiveness (nguoi choi co thuc su hieu). Game da co suc hut nho monster de
thuong va pixel art, nhung can muc tieu ro hon va cach xac nhan nguoi choi da hieu
vong lap tu thu hoach den trieu hoi.

Nguon: [GDC - Start Right, Start Fun](https://www.gdcvault.com/play/1034824/Start-Right-Start-Fun-Unveiling)

### Tien trinh nhin thay duoc

Tien trinh truc quan can cho biet ca hanh dong tiep theo va hanh trinh dai han.
Fog va Book da co cau truc dai han, nhung chua chi ro hanh dong huu ich tiep theo
trong moi phien choi.

Nguon: [GDC - From Zero to Hero](https://www.gdcvault.com/play/1026271/From-Zero-to-Hero-Visualizing)

### Lua chon co y nghia

Quyen tu quyet giup duy tri hung thu. Lua chon nen den tu viec chon Helper
monster, recipe hoac zone muon theo duoi va request muon hoan thanh. Khong can
xay mot he chi so RPG lon.

Nguon: [GDC - The Importance of Player Autonomy](https://www.gdcvault.com/play/1021257/The-Importance-of-Player-Autonomy)

### Kiem soat yeu to ngau nhien

Suu tap hoan toan ngau nhien co the tao chuoi duplicate gay that vong. Co che
pseudo-random hoac chong xui van giu bat ngo nhung giup hanh trinh suu tap cong
bang va de can bang hon.

Nguon: [GDC - Random Is the Enemy](https://gdcvault.com/play/1026873/Random-Is-the-Enemy-Collating)

### Suu tap dua tren moi quan he

Neko Atsume cho thay mot nhan vat van co gia tri sau khi duoc kham pha nho so
thich, nhung lan ghe tham, album va qua ky niem. Tiny Monster Keeper co the dung
moc friendship va qua nho ma khong can ve animation moi.

Nguon:

- [Neko Atsume - How to Play](https://www.nekoatsume.com/sp/en/about.html)
- [Neko Atsume 2 - Official Site](https://www.nekoatsume.com/sp2/index_en.html)

## 3. Cac nang cap uu tien

### 3.1 Theo doi muc tieu hien tai

Hien thi mot the muc tieu nho tai mot thoi diem:

```text
Thu hoach 3 Red Berry       1/3
Nau cong thuc dau tien      0/1
Chao don monster dau tien   0/1
Choi voi Leafy              0/1
Thu thap 5 coin             2/5
Mo Zone01                   0/1
```

Moi buoc hoan thanh thuong mot it coin hoac nguyen lieu khoi dau, sau do tu mo
muc tieu tiep theo. Chuoi dau tien la tutorial tuong tac; sau do cung component
co the hien thi muc tieu tien trinh binh thuong.

Ghi chu trien khai:

- luu ID va tien do muc tieu trong save version 2 hoac phan mo rong tuong thich;
- dang ky event harvest, cooking, summon, friendship, coin va fog co san;
- chi focus/highlight object lien quan den buoc tutorial hien tai;
- khong khoa tuong tac khac sau khi nguoi choi da hieu;
- co nut thu gon de the khong che ban do.

Chi phi: art thap, code trung binh, tac dong rat cao.

### 3.2 Recipe Book va goi y theo tien trinh

Them muc Recipe vao Book. Cong thuc chua biet se he lo thong tin dan:

```text
Woodland Forage
Apple + Red Berry + ?
Goi y: Nguyen lieu con thieu moc o Zone04.
```

Trang thai de xuat:

1. Chua biet: ba silhouette va goi y biome/zone.
2. Biet mot phan: hien nguyen lieu da kham pha.
3. Da nau mot lan: hien ten mon, nguyen lieu va silhouette monster ket qua.
4. Da nhan ket qua: hien monster cung nhan do hiem.

Cooking that bai nen giup nguoi choi hoc:

- lan fail lien quan dau tien: chi ra mot nguyen lieu khong phu hop;
- fail nhieu lan: mo mot slot dung hoac dua goi y zone ro hon;
- khong goi y item thuoc zone chua the tiep can;
- giu qua trinh cooking fail ba giay da co.

Chi phi: art thap, code trung binh, tac dong rat cao.

### 3.3 Ky nang thu dong cua Helper monster

Cho phep chon toi da ba Helper monster. Moi helper co mot hieu ung nho:

| Nhom | Passive vi du |
| --- | --- |
| La/vuon cay | Giam timer bush va apple |
| Mushroom | Giam timer mushroom |
| Nong trai | Co co hoi them mot vegetable drop |
| Bamboo/bee | Giam timer bamboo hoac Honey Butter |
| Crystal/magic | Giam timer crystal va glowing mushroom |
| Cooking | Giam thoi gian nau |
| Social | Tang friendship tu Feed/Play |
| Coin | Tang suc chua coin, khong tang manh thu nhap |

| So sao | Hieu luc passive |
| ---: | ---: |
| 1 | 5% |
| 2 | 10% |
| 3 | 15% |

Quy tac:

- chi helper duoc chon moi ap dung bonus, khong cong don ca 27 monster;
- hien timer cuoi cung de hieu ung minh bach;
- khong giam timer duoi nguong an toan;
- uu tien suc chua coin thay vi multiplier thu nhap lon;
- duplicate co gia tri vi so sao lam passive manh hon.

Chi phi: art rat thap, code trung binh, tac dong rat cao.

### 3.4 Co che chong xui cho cooking

Giu weight cong thuc hien tai nhung them bao ve an:

- sau bon lan nau thanh cong khong co monster moi, uu tien ket qua chua kham pha;
- sau tam ket qua khong co rare, tang dan weight rare;
- reset bo dem khi ket qua duoc bao ve xuat hien;
- neu da suu tap du, tiep tuc duplicate/tang sao binh thuong;
- luu bo dem de dong game khong reset chong xui.

Khong cong bo nguong chinh xac truoc khi balance test. Chi can cho nguoi choi biet
nau lap lai se cai thien co hoi kham pha.

Chi phi: khong can art, code thap den trung binh, tac dong cao.

### 3.5 Moc friendship va Memento

| Friendship | Phan thuong |
| ---: | --- |
| 25 | Mo thong tin nguyen lieu yeu thich hoac noi song |
| 50 | Monster tang mot nguyen lieu hien co |
| 75 | Mo cau profile, danh hieu hoac effect nho |
| 100 | Nhan Memento duoc luu trong Book |

Memento co the dung icon item hien co voi ten rieng va mot cau mo ta. Ban dau
khong can sprite rieng cho tung monster. He thong bien friendship thanh muc tieu
suu tap thay vi chi la mot con so.

Chi phi: art rat thap, noi dung chu trung binh, code thap den trung binh.

## 4. Cac nang cap thu cap

### 4.1 Zone Mastery

Moi zone co ba muc tieu co dinh:

```text
Zone02 - Farm
[x] Thu hoach pumpkin, eggplant va tomato
[x] Nau Harvest Stew
[ ] Kham pha du ba monster cua Harvest Stew
```

Phan thuong nen vua phai: coin, bonus timer nho tai zone hoac dau mastery trong
Book. Khong tao them tien te moi.

### 4.2 Bang request cua monster

Tao toi da ba yeu cau dua tren monster da co va nguyen lieu dang tiep can:

```text
Leafy muon 2 Red Berry.
Dewli muon 1 Purple Berry va 1 Green Mushroom.
Kabuto muon Harvest Stew.
```

Thuong friendship, coin hoac nguyen lieu huu ich. Cho doi mot request mien phi va
khong bao gio yeu cau item thuoc zone dang khoa.

### 4.3 Thoi tiet nhe

- Mua nhe: mushroom nhanh hon 10%.
- Ngay nang: vegetable nhanh hon 10%.
- Dem pha le: tai nguyen hang dong nhanh hon 10%.
- Buoi sang cua ong: Honey Butter nhanh hon 10%.

ParticleSystem mua tu TinyVillage co the tai su dung sau khi kiem tra texture,
material, shader, license va overdraw tren dien thoai. Thoi tiet khac co the bat
dau bang icon, tint mau va banner chu.

### 4.4 Hanh vi tu nhien cua monster

- hai monster gan nhau thinh thoang quay mat va hien tim;
- monster doi luc di ve Cooking Pot khi dang nau;
- monster tiep can resource phu hop voi nhom;
- monster friendship cao doi khi de lai qua;
- monster phan ung khi nguoi choi thu hoach gan do.

Hanh vi khong duoc can click, fog blocker, vi tri save hoac navigation.

## 5. Cac tinh nang nen hoan

- he thong chien dau hoan chinh;
- trang bi, khac he va chi so combat phuc tap;
- xay dung hoac dat decoration tu do;
- PvP, guild, chat va leaderboard;
- them nhieu loai tien te;
- energy gioi han hoat dong;
- lich diem danh 30 ngay;
- push notification thuong xuyen hoac ep buoc;
- ve lai map quy mo lon theo mua.

Nhung he thong nay tang pham vi art, UI, balance, save, QA va policy nhung khong
giai quyet van de dau game va cong dung monster hien tai.

## 6. Thu tu trien khai de xuat

### Giai doan A - Lam ro phien choi dau

1. Objective/tutorial tracker.
2. Recipe Book va goi y theo tien trinh.
3. Feedback cho hanh dong bi chan, chua san sang hoac thieu tai nguyen.
4. Do funnel tutorial.

### Giai doan B - Tang gia tri suu tap

1. Chon Helper monster.
2. Passive ability tang theo so sao.
3. Co che chong xui cooking.
4. Moc friendship va Memento.

### Giai doan C - Muc tieu dai han

1. Zone Mastery.
2. Monster request.
3. Thoi tiet nhe.
4. Hanh vi tu nhien cua monster.

Chi danh gia combat sau khi test Giai doan A va B. Neu monster van thieu muc dich
chu dong, hay lam auto-battle thanh prototype rieng thay vi gan ngay vao release.

## 7. Ke hoach kiem chung

Theo doi funnel cua phien choi dau:

```text
vao GameplayScene
-> thu hoach resource dau tien
-> nhat drop dau tien
-> mo cooking panel
-> bat dau nau lan dau
-> nhan ket qua cooking
-> trieu hoi monster dau tien
-> tuong tac monster lan dau
-> nhan coin dau tien
-> mo Zone01
```

Ghi ty le hoan thanh va thoi gian tung buoc. Unity Analytics Funnel giup phat
hien noi nguoi choi roi khoi hanh trinh va kiem tra tutorial co de hieu hay khong.

Nguon:

- [Unity Analytics - Funnels](https://docs.unity.com/en-us/analytics/funnels/funnels)
- [Unity Analytics - Retention](https://docs.unity.com/en-us/analytics/dashboards/dashboards)

Cau hoi playtest:

1. Nguoi moi co trieu hoi monster dau ma khong can huong dan bang loi?
2. Lan trieu hoi dau co xay ra trong nam phut?
3. Nguoi choi co hieu vi sao recipe that bai?
4. Sau muoi phut, nguoi choi co noi duoc muc tieu tiep theo?
5. Nhan duplicate co con huu ich?
6. Khi cho timer dai co it nhat mot hanh dong co y nghia?
7. Nguoi choi co quay lai khi biet cooking va resource chay offline?

Khong dieu chinh economy theo mot tester. Can so sanh clean account, offline
return va funnel truoc khi thay doi dong thoi timer va gia mo zone.

## 8. Khuyen nghi cuoi cung

Ba nang cap co gia tri cao nhat nhung can it art nhat:

1. Objective/tutorial tracker.
2. Kham pha recipe va goi y theo ngu canh.
3. Passive cua Helper monster tang theo so sao.

Ba he thong nay giai quyet thieu dinh huong, recipe thanh doan mo va monster chua
co nhieu cong dung. Nen lam truoc combat hoac mot dot san xuat noi dung lon.
