// All variants are derived from the same PNG bytes. Only green skin RGB changes;
// alpha, frame coordinates, eyes, feet and animation timing remain identical.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs'
import { inflateSync, deflateSync } from 'node:zlib'
import assert from 'node:assert/strict'
const root = new URL('../src/assets/game/frogs/', import.meta.url)
const names = ['idle-first', 'idle', 'jump', 'swim', 'attack']
const variants = { 'heavy-frog': [242, 153, 85], 'sticky-frog': [49, 224, 87], 'sniper-frog': [46, 139, 87] }
const crcTable = Array.from({ length: 256 }, (_, n) => {
  for (let k = 0; k < 8; k++) n = n & 1 ? 0xedb88320 ^ (n >>> 1) : n >>> 1
  return n >>> 0
})
function chunk(type, data) {
  const label = Buffer.from(type), out = Buffer.alloc(data.length + 12)
  out.writeUInt32BE(data.length); label.copy(out, 4); data.copy(out, 8)
  let crc = 0xffffffff
  for (const v of Buffer.concat([label, data])) crc = crcTable[(crc ^ v) & 255] ^ (crc >>> 8)
  out.writeUInt32BE((crc ^ 0xffffffff) >>> 0, data.length + 8)
  return out
}
function decode(bytes) {
  let pos = 8, header, parts = []
  while (pos < bytes.length) {
    const len = bytes.readUInt32BE(pos), type = bytes.toString('ascii', pos + 4, pos + 8)
    const data = bytes.subarray(pos + 8, pos + 8 + len)
    if (type === 'IHDR') header = data
    if (type === 'IDAT') parts.push(data)
    pos += len + 12
  }
  assert.equal(header[8], 8); assert.equal(header[9], 6, 'Expected 8-bit RGBA master')
  assert.equal(header[12], 0, 'Interlacing is not supported')
  const width = header.readUInt32BE(0), height = header.readUInt32BE(4), stride = width * 4
  const raw = inflateSync(Buffer.concat(parts)), pixels = Buffer.alloc(width * height * 4)
  for (let y = 0; y < height; y++) {
    const filter = raw[y * (stride + 1)]
    for (let x = 0; x < stride; x++) {
      const i = y * stride + x, a = x >= 4 ? pixels[i - 4] : 0
      const b = y ? pixels[i - stride] : 0, c = y && x >= 4 ? pixels[i - stride - 4] : 0
      const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c)
      const predictor = [0, a, b, Math.floor((a + b) / 2), pa <= pb && pa <= pc ? a : pb <= pc ? b : c][filter]
      assert.notEqual(predictor, undefined)
      pixels[i] = (raw[y * (stride + 1) + 1 + x] + predictor) & 255
    }
  }
  return { width, height, header, pixels }
}
function encode({ width, height, header, pixels }) {
  const stride = width * 4, raw = Buffer.alloc((stride + 1) * height)
  for (let y = 0; y < height; y++) pixels.copy(raw, y * (stride + 1) + 1, y * stride, (y + 1) * stride)
  return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]), chunk('IHDR', header), chunk('IDAT', deflateSync(raw)), chunk('IEND', Buffer.alloc(0))])
}
function isSkin(r, g, b) { return g > r * 1.05 && g > b * 1.3 && g > 35 }
function tintSkin(r, g, b, target) {
  const value = Math.max(r,g,b) / 255
  const highlight = Math.min(r,g,b) / Math.max(r,g,b)
  return target.map(channel => Math.round(Math.min(255,
    (channel + (255 - channel) * highlight) * value / .82)))
}
const verify = process.argv.includes('--check')
for (const name of names) {
  const master = decode(readFileSync(new URL(`common-frog/${name}.png`, root)))
  for (const [folder, target] of Object.entries(variants)) {
    const pixels = Buffer.from(master.pixels)
    for (let i = 0; i < pixels.length; i += 4) {
      const [r,g,b] = pixels.subarray(i,i+3)
      if (isSkin(r,g,b)) pixels.set(tintSkin(r,g,b,target), i)
    }
    const file = new URL(`${folder}/${name}.png`, root)
    if (verify) {
      const actual = decode(readFileSync(file))
      assert.equal(actual.width, master.width); assert.equal(actual.height, master.height)
      assert.deepEqual(actual.pixels, pixels, `${folder}/${name} differs from the palette-only master`)
    } else {
      mkdirSync(new URL(`${folder}/`, root), { recursive: true })
      writeFileSync(file, encode({ ...master, pixels }))
    }
  }
}
console.log(verify ? 'PASS: palette variants preserve alpha, geometry and expected skin colors.' : 'Generated palette variants from the common frog master.')
