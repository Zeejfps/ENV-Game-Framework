// WebGL2 interop shim for ZGF.Gui.Web. The .NET side (Rendering/Webgl2.cs) drives
// this through [JSImport]. To let the C# backend mirror the desktop GL backend's
// integer-handle model, this module keeps handle tables (int -> WebGL object) and
// exposes a flat, primitive-only function surface. Index 0 is reserved as "no
// object" in every table.
//
// STATUS: scaffolding — authored without a browser to run it against. Treat the
// first `dotnet run` as the validation step (docs/web-font-rendering.md §7-§8).

let gl = null;

const buffers = [null];
const vaos = [null];
const programs = [null];
const shaders = [null];
const textures = [null];
const uniforms = [null];

function put(table, obj) { table.push(obj); return table.length - 1; }

export function init(canvasSelector) {
    const canvas = document.querySelector(canvasSelector);
    if (!canvas) return 0;
    gl = canvas.getContext('webgl2', { alpha: true, premultipliedAlpha: false, antialias: false });
    return gl ? 1 : 0;
}

// ---- per-frame state ----
export function viewport(x, y, w, h) { gl.viewport(x, y, w, h); }
export function clearColor(r, g, b, a) { gl.clearColor(r, g, b, a); }
export function clear(mask) { gl.clear(mask); }
export function enable(cap) { gl.enable(cap); }
export function disable(cap) { gl.disable(cap); }
export function blendFunc(s, d) { gl.blendFunc(s, d); }
export function activeTexture(t) { gl.activeTexture(t); }

// ---- shaders / programs ----
export function createShader(type) { return put(shaders, gl.createShader(type)); }
export function shaderSource(id, src) { gl.shaderSource(shaders[id], src); }
export function compileShader(id) { gl.compileShader(shaders[id]); }
export function getShaderCompiled(id) { return gl.getShaderParameter(shaders[id], gl.COMPILE_STATUS) ? 1 : 0; }
export function getShaderInfoLog(id) { return gl.getShaderInfoLog(shaders[id]) || ''; }
export function createProgram() { return put(programs, gl.createProgram()); }
export function attachShader(p, s) { gl.attachShader(programs[p], shaders[s]); }
export function linkProgram(p) { gl.linkProgram(programs[p]); }
export function getProgramLinked(p) { return gl.getProgramParameter(programs[p], gl.LINK_STATUS) ? 1 : 0; }
export function getProgramInfoLog(p) { return gl.getProgramInfoLog(programs[p]) || ''; }
export function useProgram(p) { gl.useProgram(p === 0 ? null : programs[p]); }

export function getUniformLocation(p, name) {
    const loc = gl.getUniformLocation(programs[p], name);
    return loc ? put(uniforms, loc) : -1;
}
export function uniform1i(loc, v) { gl.uniform1i(uniforms[loc], v); }
export function uniformMatrix4fv(loc, view) {
    gl.uniformMatrix4fv(uniforms[loc], false, new Float32Array(view.slice().buffer));
}
export function getUniformBlockIndex(p, name) {
    const idx = gl.getUniformBlockIndex(programs[p], name);
    return idx === gl.INVALID_INDEX ? -1 : idx;
}
export function uniformBlockBinding(p, blockIndex, binding) {
    gl.uniformBlockBinding(programs[p], blockIndex, binding);
}

// ---- buffers ----
export function createBuffer() { return put(buffers, gl.createBuffer()); }
export function bindBuffer(target, b) { gl.bindBuffer(target, b === 0 ? null : buffers[b]); }
export function bufferDataSize(target, size, usage) { gl.bufferData(target, size, usage); }
export function bufferSubData(target, offset, view) { gl.bufferSubData(target, offset, view.slice()); }
export function bindBufferBase(target, index, b) { gl.bindBufferBase(target, index, buffers[b]); }

// ---- vertex arrays ----
export function createVertexArray() { return put(vaos, gl.createVertexArray()); }
export function bindVertexArray(v) { gl.bindVertexArray(v === 0 ? null : vaos[v]); }
export function enableVertexAttribArray(i) { gl.enableVertexAttribArray(i); }
export function vertexAttribPointer(i, size, type, normalized, stride, offset) {
    gl.vertexAttribPointer(i, size, type, normalized !== 0, stride, offset);
}
export function vertexAttribIPointer(i, size, type, stride, offset) {
    gl.vertexAttribIPointer(i, size, type, stride, offset);
}
export function vertexAttribDivisor(i, divisor) { gl.vertexAttribDivisor(i, divisor); }

// ---- textures ----
export function createTexture() { return put(textures, gl.createTexture()); }
export function bindTexture(target, t) { gl.bindTexture(target, t === 0 ? null : textures[t]); }
export function texImage2DSize(target, level, internalFormat, w, h, border, format, type) {
    gl.texImage2D(target, level, internalFormat, w, h, border, format, type, null);
}
export function texImage2DData(target, level, internalFormat, w, h, border, format, type, view) {
    gl.texImage2D(target, level, internalFormat, w, h, border, format, type, view.slice());
}
export function texSubImage2D(target, level, x, y, w, h, format, type, view) {
    gl.texSubImage2D(target, level, x, y, w, h, format, type, view.slice());
}
export function texParameteri(target, pname, param) { gl.texParameteri(target, pname, param); }
export function pixelStorei(pname, param) { gl.pixelStorei(pname, param); }

// ---- draw ----
export function drawArraysInstanced(mode, first, count, instanceCount) {
    gl.drawArraysInstanced(mode, first, count, instanceCount);
}
