// =====================================================================
//  Gastos de casa - frontend
//  JavaScript "vanilla" (sin frameworks). Habla con la API por fetch.
// =====================================================================

const REFRESCO_MS = 15000; // cada 15 s trae lo que cargaron los demás

const estado = {
  usuario: null,          // { id, nombre }
  categorias: [],
  mes: primerDiaDelMes(new Date()),
  editandoId: null,       // id del movimiento que se está editando (o null)
  timer: null,
};

const $ = (sel) => document.querySelector(sel);

// ---------------------------------------------------------------------
//  Utilidades
// ---------------------------------------------------------------------

const formatoPlata = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
const plata = (n) => formatoPlata.format(n);
// Para los totales de arriba: sin centavos, así entran en la pantalla del celular.
const formatoPlataCorta = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });
const plataCorta = (n) => formatoPlataCorta.format(n);

function primerDiaDelMes(fecha) {
  return new Date(fecha.getFullYear(), fecha.getMonth(), 1);
}

// Date -> "2026-11-23" (en hora local, no UTC: si no, a la noche te cambia de día)
function aISO(fecha) {
  const m = String(fecha.getMonth() + 1).padStart(2, '0');
  const d = String(fecha.getDate()).padStart(2, '0');
  return `${fecha.getFullYear()}-${m}-${d}`;
}

function rangoDelMes() {
  const desde = estado.mes;
  const hasta = new Date(desde.getFullYear(), desde.getMonth() + 1, 0); // día 0 del mes siguiente = último día
  return { desde: aISO(desde), hasta: aISO(hasta) };
}

// Acepta cómo escribimos en Argentina: "1500", "1500,50", "1.500,50", "1.500", y también "1500.50"
function leerMonto(texto) {
  let s = texto.trim().replace(/\s|\$/g, '');
  if (s.includes(',')) s = s.replace(/\./g, '').replace(',', '.');
  else if (/^\d{1,3}(\.\d{3})+$/.test(s)) s = s.replace(/\./g, '');
  const n = Number(s);
  return Number.isFinite(n) ? Math.round(n * 100) / 100 : NaN;
}

function avisar(mensaje) {
  const el = $('#aviso');
  el.textContent = mensaje;
  el.hidden = false;
  clearTimeout(avisar.t);
  avisar.t = setTimeout(() => (el.hidden = true), 2500);
}

// Crea un elemento con texto. Usar textContent (y no innerHTML) con datos
// del usuario evita que alguien "inyecte" HTML/JS en una descripción (XSS).
function el(tag, clase, texto) {
  const e = document.createElement(tag);
  if (clase) e.className = clase;
  if (texto !== undefined) e.textContent = texto;
  return e;
}

// ---------------------------------------------------------------------
//  Llamadas a la API
// ---------------------------------------------------------------------

async function api(metodo, url, cuerpo) {
  const resp = await fetch(url, {
    method: metodo,
    headers: cuerpo ? { 'Content-Type': 'application/json' } : {},
    body: cuerpo ? JSON.stringify(cuerpo) : undefined,
  });

  if (resp.status === 401 && !url.startsWith('/api/auth/')) {
    mostrarLogin();                       // se venció la sesión
    throw new Error('Tu sesión expiró. Volvé a ingresar.');
  }
  if (resp.status === 204) return null;

  const datos = await resp.json().catch(() => null);
  if (!resp.ok) {
    // Errores de la BLL vienen como { error: "..." }; los de validación de ASP.NET como { errors: {...} }
    const msg = datos?.error
      ?? (datos?.errors ? Object.values(datos.errors).flat().join(' ') : null)
      ?? `Error ${resp.status}`;
    throw new Error(msg);
  }
  return datos;
}

// ---------------------------------------------------------------------
//  Login / registro
// ---------------------------------------------------------------------

let modoLogin = 'login';

function cambiarModo(modo) {
  modoLogin = modo;
  document.querySelectorAll('.pestania').forEach((b) => b.classList.toggle('activa', b.dataset.modo === modo));
  const registro = modo === 'registro';
  document.querySelector('.solo-registro').hidden = !registro;
  document.querySelector('[name=codigoCasa]').required = registro;
  document.querySelector('[name=password]').autocomplete = registro ? 'new-password' : 'current-password';
  $('#boton-login').textContent = registro ? 'Crear cuenta' : 'Ingresar';
  $('#error-login').textContent = '';
}

async function enviarLogin(ev) {
  ev.preventDefault();
  const datos = Object.fromEntries(new FormData(ev.target));
  const boton = $('#boton-login');
  boton.disabled = true;
  $('#error-login').textContent = '';
  try {
    const url = modoLogin === 'registro' ? '/api/auth/registro' : '/api/auth/login';
    estado.usuario = await api('POST', url, datos);
    ev.target.reset();
    await mostrarApp();
  } catch (e) {
    $('#error-login').textContent = e.message;
  } finally {
    boton.disabled = false;
  }
}

function mostrarLogin() {
  clearInterval(estado.timer);
  estado.usuario = null;
  $('#pantalla-app').hidden = true;
  $('#pantalla-login').hidden = false;
}

async function salir() {
  await api('POST', '/api/auth/logout').catch(() => {});
  mostrarLogin();
}

// ---------------------------------------------------------------------
//  App principal
// ---------------------------------------------------------------------

async function mostrarApp() {
  $('#pantalla-login').hidden = true;
  $('#pantalla-app').hidden = false;
  $('#nombre-usuario').textContent = estado.usuario.nombre;

  const [categorias, usuarios] = await Promise.all([api('GET', '/api/categorias'), api('GET', '/api/usuarios')]);
  estado.categorias = categorias;
  llenarCombo($('#filtro-usuario'), usuarios, 'Todos');
  llenarCombo($('#filtro-categoria'), categorias, 'Todas las categorías');
  limpiarFormulario();

  await refrescar();
  clearInterval(estado.timer);
  estado.timer = setInterval(() => refrescar(true), REFRESCO_MS);
}

function llenarCombo(select, items, textoTodos) {
  const valorPrevio = select.value;
  select.replaceChildren(new Option(textoTodos, ''));
  items.forEach((i) => select.add(new Option(i.nombre, i.id)));
  select.value = valorPrevio;
}

function tipoElegido() {
  return document.querySelector('[name=tipo]:checked').value;
}

// El combo de categorías muestra solo las del tipo elegido (gasto o ingreso).
function actualizarCategoriasDelForm(idSeleccionado) {
  const select = document.querySelector('[name=categoriaId]');
  const tipo = tipoElegido();
  select.replaceChildren();
  estado.categorias.filter((c) => c.tipo === tipo).forEach((c) => select.add(new Option(c.nombre, c.id)));
  if (idSeleccionado) select.value = idSeleccionado;
}

function limpiarFormulario() {
  const form = $('#form-movimiento');
  form.reset();
  form.fecha.value = aISO(new Date());
  estado.editandoId = null;
  $('#titulo-form').textContent = 'Nuevo movimiento';
  $('#boton-guardar').textContent = 'Guardar';
  $('#boton-cancelar').hidden = true;
  $('#error-form').textContent = '';
  document.querySelectorAll('.item.editando').forEach((li) => li.classList.remove('editando'));
  actualizarCategoriasDelForm();
}

async function guardarMovimiento(ev) {
  ev.preventDefault();
  const form = ev.target;
  const monto = leerMonto(form.monto.value);
  if (!(monto > 0)) {
    $('#error-form').textContent = 'Ingresá un monto válido, por ejemplo 1500 o 1.500,50';
    return;
  }

  const cuerpo = {
    tipo: tipoElegido(),
    monto,
    fecha: form.fecha.value,
    descripcion: form.descripcion.value,
    categoriaId: Number(form.categoriaId.value),
  };

  const boton = $('#boton-guardar');
  boton.disabled = true;
  try {
    if (estado.editandoId) {
      await api('PUT', `/api/movimientos/${estado.editandoId}`, cuerpo);
      avisar('Cambios guardados');
    } else {
      await api('POST', '/api/movimientos', cuerpo);
      avisar('Movimiento guardado ✔');
    }
    // Si cargaste algo de otro mes, saltamos a ese mes para que lo veas.
    const [a, m] = cuerpo.fecha.split('-').map(Number);
    estado.mes = new Date(a, m - 1, 1);
    limpiarFormulario();
    await refrescar();
  } catch (e) {
    $('#error-form').textContent = e.message;
  } finally {
    boton.disabled = false;
  }
}

function empezarEdicion(mov, li) {
  const form = $('#form-movimiento');
  estado.editandoId = mov.id;
  form.querySelector(`[name=tipo][value=${mov.tipo}]`).checked = true;
  actualizarCategoriasDelForm(mov.categoriaId);
  form.monto.value = String(mov.monto).replace('.', ',');
  form.fecha.value = mov.fecha;
  form.descripcion.value = mov.descripcion;
  $('#titulo-form').textContent = 'Editar movimiento';
  $('#boton-guardar').textContent = 'Guardar cambios';
  $('#boton-cancelar').hidden = false;
  document.querySelectorAll('.item.editando').forEach((x) => x.classList.remove('editando'));
  li.classList.add('editando');
  form.scrollIntoView({ behavior: 'smooth', block: 'center' });
  form.monto.focus({ preventScroll: true });
}

async function borrarMovimiento(mov) {
  const texto = `¿Borrar "${mov.descripcion || mov.categoriaNombre}" por ${plata(mov.monto)}?`;
  if (!confirm(texto)) return;
  try {
    await api('DELETE', `/api/movimientos/${mov.id}`);
    if (estado.editandoId === mov.id) limpiarFormulario();
    avisar('Movimiento borrado');
    await refrescar();
  } catch (e) {
    avisar(e.message);
  }
}

// ---------------------------------------------------------------------
//  Traer datos y dibujar
// ---------------------------------------------------------------------

async function refrescar(silencioso = false) {
  if (!estado.usuario) return;
  const { desde, hasta } = rangoDelMes();

  // Los filtros de la lista NO afectan los totales: los totales son siempre del mes completo.
  const filtros = new URLSearchParams({ desde, hasta });
  if ($('#filtro-usuario').value) filtros.set('usuarioId', $('#filtro-usuario').value);
  if ($('#filtro-tipo').value) filtros.set('tipo', $('#filtro-tipo').value);
  if ($('#filtro-categoria').value) filtros.set('categoriaId', $('#filtro-categoria').value);

  try {
    const [resumen, movimientos] = await Promise.all([
      api('GET', `/api/movimientos/resumen?desde=${desde}&hasta=${hasta}`),
      api('GET', `/api/movimientos?${filtros}`),
    ]);
    dibujarMes();
    dibujarResumen(resumen);
    dibujarLista(movimientos);
  } catch (e) {
    if (!silencioso) avisar(e.message);
  }
}

function dibujarMes() {
  const texto = estado.mes.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
  $('#titulo-mes').textContent = texto[0].toUpperCase() + texto.slice(1); // "Noviembre de 2026"
}

function dibujarResumen(r) {
  $('#total-ingresos').textContent = plataCorta(r.totalIngresos);
  $('#total-gastos').textContent = plataCorta(r.totalGastos);
  const balance = $('#total-balance');
  balance.textContent = plataCorta(r.balance);
  balance.style.color = r.balance < 0 ? 'var(--rojo)' : 'var(--verde)';

  dibujarBarras($('#grafico-categorias'), r.porCategoria.filter((g) => g.tipo === 'Gasto'));
  dibujarBarras($('#grafico-personas'), r.porUsuario.filter((g) => g.tipo === 'Gasto'));
}

function dibujarBarras(contenedor, grupos) {
  contenedor.replaceChildren();
  if (grupos.length === 0) {
    contenedor.append(el('p', 'vacio', 'Todavía no hay gastos este mes.'));
    return;
  }
  const max = Math.max(...grupos.map((g) => g.total));
  for (const g of grupos) {
    const fila = el('div', 'barra-fila');
    const pista = el('div', 'barra-pista');
    const relleno = el('div', 'barra-relleno');
    relleno.style.width = `${(g.total / max) * 100}%`;
    pista.append(relleno);
    fila.append(el('span', 'barra-nombre', g.nombre), pista, el('span', '', plata(g.total)));
    contenedor.append(fila);
  }
}

function dibujarLista(movimientos) {
  const ul = $('#lista-movimientos');
  ul.replaceChildren();
  if (movimientos.length === 0) {
    ul.append(el('li', 'vacio', 'No hay movimientos para mostrar.'));
    return;
  }

  for (const m of movimientos) {
    const li = el('li', 'item');
    if (m.id === estado.editandoId) li.classList.add('editando');

    const info = el('div', 'item-info');
    info.append(
      el('div', 'item-titulo', m.descripcion || m.categoriaNombre),
      el('div', 'item-detalle', `${formatearFecha(m.fecha)} · ${m.categoriaNombre} · ${m.usuarioNombre}`)
    );

    const signo = m.tipo === 'Gasto' ? '−' : '+';
    const monto = el('div', `item-monto ${m.tipo.toLowerCase()}`, `${signo} ${plata(m.monto)}`);
    li.append(info, monto);

    // Solo quien lo cargó puede editarlo o borrarlo (la API también lo controla).
    if (m.usuarioId === estado.usuario.id) {
      const acciones = el('div', 'item-acciones');
      const editar = el('button', '', '✏️');
      editar.type = 'button';
      editar.title = 'Editar';
      editar.onclick = () => empezarEdicion(m, li);
      const borrar = el('button', '', '🗑️');
      borrar.type = 'button';
      borrar.title = 'Borrar';
      borrar.onclick = () => borrarMovimiento(m);
      acciones.append(editar, borrar);
      li.append(acciones);
    }
    ul.append(li);
  }
}

function formatearFecha(iso) {
  const [, m, d] = iso.split('-');
  return `${d}/${m}`;
}

function cambiarMes(delta) {
  estado.mes = new Date(estado.mes.getFullYear(), estado.mes.getMonth() + delta, 1);
  refrescar();
}

// ---------------------------------------------------------------------
//  Arranque
// ---------------------------------------------------------------------

document.querySelectorAll('.pestania').forEach((b) => (b.onclick = () => cambiarModo(b.dataset.modo)));
$('#form-login').addEventListener('submit', enviarLogin);
$('#boton-salir').onclick = salir;
$('#form-movimiento').addEventListener('submit', guardarMovimiento);
$('#boton-cancelar').onclick = limpiarFormulario;
document.querySelectorAll('[name=tipo]').forEach((r) => (r.onchange = () => actualizarCategoriasDelForm()));
$('#mes-anterior').onclick = () => cambiarMes(-1);
$('#mes-siguiente').onclick = () => cambiarMes(1);
['#filtro-usuario', '#filtro-tipo', '#filtro-categoria'].forEach((s) => ($(s).onchange = () => refrescar()));

// Al volver a la pestaña (por ej. desbloquear el celular), actualizar enseguida.
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') refrescar(true);
});

// ¿Ya hay sesión abierta? (la cookie dura 30 días)
(async () => {
  try {
    estado.usuario = await api('GET', '/api/auth/yo');
    await mostrarApp();
  } catch {
    mostrarLogin();
  }
})();
