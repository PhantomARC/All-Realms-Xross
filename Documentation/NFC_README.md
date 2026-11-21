<!-- Sheet Anchors -->
<!-- Pretty useful for editing too! -->
[title]: #arxs-nfc-datasheet
[toc]: #table-of-contents
[sys]: #system-page-specifications
[sn]: #serial-number
[staticlock]: #static-lock-bytes
[dynamiclock]: #dynamic-lock-bytes
[capctr]: #capability-container
[cfgpg]: #configuration-pages
[access]: #access
[auth0]: #auth0
[pwd]: #password
[pack]: #password-acknowledgement
[rfui]: #rfui
[src]: https://www.nxp.com/docs/en/data-sheet/NTAG213_215_216.pdf
[arxdata]: #arx-data
[arxsteps]:#arx-write-steps
[arxhexid]: #hex-id
[arxmfgid]: #manufacture-supplier-id
[arxmfgdata]: #manufacturing-index
[arxpdata]: #physical-card-data
[arxslb]: #arx-static-lock-bytes
[arxrune]: #runes
[arxrunestat]: #base-rune-stats
[arxprng]: #unit-prng-hash
[arxauth0]:#arx-auth0
[arxaccess]:#arx-access
[arxpwd]:#arx-password
[arxpack]:#arx-pack

# ARX's NFC Datasheet
<!-- Blurb -->
> All Realms Xross cards use NTAG216 chips to store and retrieve data.\
The primary intent of ARXs are to function as character passports across games.\
The tags are non-volatile, cost-efficient and sizeable, but are susceptible to moisture.\
\
NTAG216s have 231 pages of data, with 222 pages of writable user data. \
Each page can store 4 bytes of data, for a total of 888 user data bytes.\
They are rated for 100,000 write cycles and 10 years of data retention.\
\
Last updated on 11/19/2026. Data sourced from [NXP Semiconductors][src] and independently tested and verified.

## Terms and Notations

> For NTAG216s, pages are indexed from 0-230, which correspond to the pages 1-231.

> Bytes in each page are indexed from 0-3, which correspond to the first through fourth byte.

> I notate bytes as `#x#`, where the first # represents the page index and the second represents the relative byte index, in decimal.\
 (e.g. the fourth byte of the 231st page would be notated as `230x3`)

## Table of Contents
<!-- ToC -->
- [System Page Specifications][sys]
- [ARX Data][arxdata]
- [ARX Write Steps][arxsteps]

## System Page Specifications

> This section details system page properties and byte assignments. All data here applies to any NTAG216 and is not limited to ARX-coded NTAG216s in particular.

NTAG216 system pages are located at the front and back ends of the page list.
These pages span from 0-3, and from 226-230, for a total of 9 pages.
System pages cannot store user data, as they are integral to the card's ability to function. 
Note that all NTAG21X have system pages spanning 0-3, which are used for identification purposes.

Below is a table detailing each page's purpose. 


| Page | Purpose | Editable? |
| :---: | :--- | :---: |
| 0 | [Serial Number][sn] | No |
| 1 | [Serial Number][sn] | No |
| 2 | [Serial Number][sn] + [Static Lock Bytes][staticlock] | Yes* |
| 3 | [Capability Container][capctr] | Yes* |


The following pages are exclusively for the NTAG216.

| Page | Purpose | Editable? |
| :---: | :--- | :---: |
| 226 | [Dynamic Lock Bytes][dynamiclock] + [RFUI][rfui] | Yes* |
| 227 | [Configuration Page][cfgpg] | Yes |
| 228 | [Configuration Page][cfgpg] | Yes |
| 229 | [Password][pwd] | Yes |
| 230 | [Password Acknowledgement][pack] + [RFUI][rfui] | Yes* |

### Serial Number

The serial number is a unique address spanning the first three pages of the tag and is used for identification purposes.
<b>These bytes are write-protected.</b>

| Page | Byte 0 | Byte 1 | Byte 2 | Byte 3 |
| :---: | :---: | :---: | :---: | :---: |
| 0 | UID | UID | UID | CB1 |
| 1 | UID | UID | UID | UID |
| 2 | CB2 | IN | SLB | SLB |

The bytes from pages 0-1 represent the UID, with the exception of `0x3`. 
`0x3` and `2x0` are check bytes to determine authenticity. 
`2x1` is reserved for internal data. 

### Static Lock Bytes

Static Lock Bytes are a mechanism used to write-protect pages 3-15, which <b>cannot be undone</b>. 
Each bit in bytes `2x2` and `2x3` correspond to various "locks", which are detailed below:

| Byte | MSB (7) | 6 | 5 | 4 | 3 | 2 | 1 | LSB (0) |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `2x2` | Page 7 | Page 6 | Page 5 | Page 4 | Page 3 | `BL` 15-10 | `BL` 9-4 | `BL` 3 |
| `2x3` | Page 15 | Page 14 | Page 13 | Page 12 | Page 11 | Page 10 | Page 9 | Page 8 |

Bits notated `BL` are responsible for write-protecting the respective bits in each static lock byte. 
For example, Bit 6 in `2x2` is responsible for write-protecting the Bit 6 and 7 of `2x3` as well as Bits 0-3 of `2x2`. This is particularly useful when locking specific pages for immutable metadata while preserving mutability for others.

For example, If Pages 4-7 are metadata and Pages 8-9 are reserved exclusively for mutable timestamps, we can lock Bits 0-4 and 6 in `2x2`, which prevents further modification of the writability of Pages 4-9.
Note that in this example, the write command to this page would be registered as `00 00 F2 00`.

### Capability Container

Page 3 specifies the memory size as well as other memory content during delivery. `3x2` specifies the NDEF message bytes available, which we can also use to differentiate between NTAG types.

For NTAG21X, the default page contents are as follows:
| NTAG | Byte Array |
| :---: | :---: |
| 213 | `e1 10 12 00` |
| 215 | `e1 10 3e 00` |
| 216 | `e1 10 6d 00` |

While the Capability Container is not initially write-protected, it is a one-time procedure; setting a bit to logic `1` will keep it permanently locked at `1`.

### Dynamic Lock Bytes

Dynamic lock bytes are used to write-protect pages starting from 16. For the NTAG216, these are located at  page 226. There are 3 bytes that cover a span of 16 pages per bit, for a total of 840 write-protected pages.

> I'm not sure why it's worded so weirdly in the datasheet, but I've provided a table below indicating the bit to page match. Every bit that locks pages, save for one, locks 16 pages each. The datasheet also doesn't state whether Dynamic Lock bits are reversible once set to `1`, which I also haven't tested.

| Byte | MSB (7) | 6 | 5 | 4 | 3 | 2 | 1 | LSB (0) |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `226x0` | 128-143 | 112-127 | 96-111 | 80-95 | 64-79 | 48-63 | 32-47 | 16-31 |
| `226x1` | `RFUI` | `RFUI` | 224-225 | 208-223 | 192-207 | 176-191 | 160-175 | 144-159 |
| `226x2` | `RFUI` | `BL`208-225 | `BL`176-207 | `BL`144-175 | `BL`112-143 | `BL`80-111 | `BL`48-79 | `BL`16-47 |

Much like Static Lock Bytes, `BL` bits lock the respective write-protect bits in the Dynamic Lock Bytes. For example, to prevent changes to the write-status of pages 208-225, we can write `00 00 40 bd` to page 226. Again, note that setting a `BL` bit doesn't write-protect the pages; it write-protects the ability to write-protect those pages. 

> <b>The source datasheet mentions to set all bits marked RFUI to 0 when setting dynamic locks.</b>

### Configuration Pages

For NTAG216s, pages 227 to 230 configure memory access restrictions and to configure the
UID ASCII mirror feature. Below are diagrams detailing each byte's purpose.

| Page | Byte 0 | Byte 1 | Byte 2 | Byte 3 |
| :---: | :---: | :---: | :---: | :---: |
| 227 | `MIRROR` | `RFUI` | `MIRROR_PAGE` | [`AUTH0`][auth0] |
| 228 | [`ACCESS`][access] | `RFUI` | `RFUI` | `RFUI` |
| 229 | [`PWD`][pwd] | [`PWD`][pwd] | [`PWD`][pwd] | [`PWD`][pwd] |
| 230 | [`ACK`][pack] | [`ACK`][pack] | `RFUI` | `RFUI` |

> As this is a page pertaining specifically to ARX cards, I will only include relevant portions of data.
For more information on `MIRROR` and `MIRROR_PAGE`, see NXP's [source datasheet][src].

### AUTH0

AUTH0 sets the starting page address from which the password verification is required.
Valid address range for byte AUTH0 is from 0-255, or `00` - `ff`.

### ACCESS

The `ACCESS` byte contains miscellaneous accessibility permissions. The table below notates each bit's purpose. The relevant bits (for ARX cards) are explained below. For more information, see NXP's [source datasheet][src].

| Byte | MSB (7) | 6 | 5 | 4 | 3 | 2 | 1 | LSB (0) |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `228x0` | `PROT` | `CFGLCK` | `RFUI` | `NFC_CNT_EN` | `NFC_CNT_PWD_PROT` | `AUTHLIM` | `AUTHLIM` | `AUTHLIM` |

- `PROT` sets the level of protection: `0` indicates that only writing requires a password, whereas `1` indicates that both reading and writing require a password.
- `CFGLCK` sets user configuration: `1` locks user configuration, excluding `ACK` and `PWD`.

> From NXP: The `CFGLCK` bit activates the permanent write protection of the first two
configuration pages. The write lock is only activated after a power cycle of NTAG21x. If
write protection is enabled, each write attempt leads to a NAK response.

### Password

Page 229 contains a 4 byte password, which is only used when password authentication is enabled. Note that the parts protected by the password are configurable. Upon sending the correct password, a [password acknowledgement][pack] signal will be received by the scanner.

> The PWD bytes aren't read out of the memory. Any READ command will only print `00 00 00 00`.

### Password Acknowledgement

Password Acknowledgement, also known as PACK, is a signature used to verify that the tag being written to is legitimate. 

> The PACK bytes aren't read out of the memory. Any READ command will only print `00 00`.

### RFUI

`RFUI` stands for <b>Reserved for Future Use (Implemented)</b>.
> As far as I can tell, bytes marked `RFUI` do not serve any particular purpose.

## ARX Data

> This section details the data printed on each ARX NTAG. It is currently a work-in-progress.

| Page | Purpose |
| :---: | :--- |
| 2 | [Static Lock Bytes][arxslb] |
| 4 | [Manufacturer][arxmfgid]  |
| 5 | [Manufacturing Index][arxmfgdata]|
| 6 | [Hex ID][arxhexid] + [Physical Data][arxpdata] |
| 7 | [Runes][arxrune] + [Base Rune Stats][arxrunestat] |
| 8 | [Base Rune Stats][arxrunestat] |
| 9 | [Pseudo RNG Hash][arxprng] |
| 227 | [AUTH0 Configuration][arxauth0] |
| 228 | [Access Permissions][arxaccess] |
| 229 | [Password][arxpwd] |
| 230 | [Acknowledgement][arxpack] |

### ARX Static Lock Bytes 



### Manufacture Supplier ID.

Homebrew ARX NTAGs should have `00 00 00 00`. Authentic cards have `5f 41 52 58`.
### Manufacturing Index

Homebrew NTAGs should be `00 00 00 00`. Authentic cards have the first two bytes as an index from `00 01` to `ff ff` (1 to 65535) which represents their print number relative to other copies. The last 2 bytes indicate the set. Different sets can use the same card but have different artwork, so the set ID is required. E.g. Print 1 of Set 1, card 3 reads `00 01 01 03`.

### Hex ID

Refer to the card tables to set these values. The hex ID determines the actual in-game card’s identity. Generic units should be `00 00`.

### Physical Card Data

Physical card data is split into half-bytes.
A unit's labelled faction takes up 2  half-bytes (`00`).
A unit's card finish takes up 1 half-byte (`0`).
A unit's alternate artwork takes up 1 half-byte (`0`).

For generic cards, the card finish and alt-art should be set to `00`.

The table of Factions is listed below. 
> The faction list is currently a work-in-progress.

| ID | Faction | Icon |
| :---: | :---: | :---: |
| `00` | <i>Reserved</i> |  |
| `01` | <b>Solaris</b> | ![✧](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_solaris.svg) |
| `02` | <b>Gradatim</b> | ![⛭](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_gradatim.svg) |
| `03` | <b>Logia</b> | ![🗺](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_logia.svg) |
| `04` | <b>Judicia Telos</b> | ![🕮](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_judicia_telos.svg) |
| `05` | <b>Vitus</b> | ![⚗](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_vitus.svg) |
| `06` | <b>Renneaux</b> | ![🖌](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_renneaux.svg) |
| `07` | <b>Agri</b>  | ![❀](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_agri.svg) |
| `08` | <b>Paradeux</b>  | ![⚔︎](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_paradeux.svg) |
| `09` | <b>Crossover</b> | ![❖](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Faction%20Icons/faction_crossover.svg) |

The table of finishes is described below:

| ID | Finish Type | ID | Finish Type |
| :---: | :--- | :---: | :--- |
| `0` | <b>Plain Print</b> | `8` | <i>Reserved</i> |
| `1` | <b>Foil Print</b> | `9` | <i>Reserved</i> |
| `2` | <b>Lenticular Print</b> | `a` | <i>Reserved</i> |
| `3` | <i>Reserved</i> | `b` | <i>Reserved</i> |
| `4` | <i>Reserved</i> | `c` | <i>Reserved</i> |
| `5` | <i>Reserved</i> | `d` | <i>Reserved</i> |
| `6` | <i>Reserved</i> | `e` | <i>Reserved</i> |
| `7` | <i>Reserved</i> | `f` | <i>Reserved</i> |

Alternate Artworks range from `0` to `f` (for a maximum of 16 total artworks).

### Runes

| ID | Rune | Icon |
| :---: | :---: | :---: |
| `0` | <i>Combat</i> | ![⚔︎](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Rune%20Icons/rune_combat.svg) |
| `1` | <i>Travel</i> | ![👢︎](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Rune%20Icons/rune_travel.svg) |
| `2` | <i>Protection</i> | ![⛨](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Rune%20Icons/rune_protection.svg) |
| `3` | <i>Arcane</i> | ![✧](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Rune%20Icons/rune_arcane.svg) |
| `4` | <i>Logic</i> | ![🖪](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Rune%20Icons/rune_logic.svg) |
| `5` | <i>Blossom</i> | ![✾](https://github.com/PhantomARC/All-Realms-Xross/blob/main/Documentation/Rune%20Icons/rune_blossom.svg) |

### Base Rune Stats

Flat values corresponding to the unit’s base rune stats, ranging from 0-255 each.
> Note that some games may not use this data.

### Unit PRNG Hash

A random 32-bit hash should be generated for each card.

### ARX AUTH0

On Manufacture, set `00 00 00 00` to R/W protect the entire card.

### ARX ACCESS

Set to `C0 00 00 00` to lock access to read & write via password, as well as permanently locking pages 227-228.

### ARX PASSWORD

`5F 41 52 58`

### ARX PACK

`02 47 00 00` 

# ARX Write Steps

Signing Payload:

| Step | Page | Payload | Purpose |
|---: | :---: | :---: | :--- |
| 1) | 229 | `5f 41 52 58` | Set `PWD` |
| 2) | 230 | `02 47 00 00` | Set `PACK` |
| 3) | 227 | `00 00 00 00` | Set `AUTH0` |
| 4) | 228 | `c0 00 00 00` | Set `CFG_LOCK` |

Card Writing Payload:

| Step | Page | Payload | Purpose |
|---: | :---: | :---: | :--- |
| 1) | 4 | `5f 41 52 58` | Data. Manufacturer ID |
| 2) | 5 | `MFID MFID SET CARDID` | Data. Print Number + Set + Card Index |
| 3) | 6 | `HID HID Faction FA` | Data. Hex ID + Faction + Finish + Art |
| 4) | 7 | `RUNE RUNE R0 R1` | Data. Rune Orientation + Combat # + Travel # |
| 5) | 8 | `R2 R3 R4 R5` | Data. Protection # + Arcane # + Logic  # + Blossom # |
| 6) | 9 | `RNG RNG RNG RNG` | Data. PRNG Hash. Randomly Generated. |
| 7) | 2 | `00 00 f0 03` | Write-Protect Pages 4-9 |
| 8) | 2 | `00 00 03 00` | Write-Protect Lock Bytes |



<!-- Title Hook -->
[Back to Top][title]


