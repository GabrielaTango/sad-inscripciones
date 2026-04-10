export interface BaseEntity {
  id: number
  createdBy?: string
  updatedBy?: string
  createdAt?: string
  updatedAt?: string
  deletedAt?: string | null
}

export interface TipoEvento extends BaseEntity {
  nombre: string
  activo: boolean
}

export interface TipoAlumno extends BaseEntity {
  nombre: string
  activo: boolean
}

export interface Evento extends BaseEntity {
  tipoEventoId: number
  titulo: string
  descripcion?: string
  fechaInicio: string
  fechaFin: string
  fechaCierreInscripcion: string
  lugar?: string
  modalidad: string
  maxInscriptos?: number
  activo: boolean
  soloSocios?: boolean
  terminosArchivo?: string
}

export interface EventoPrecio extends BaseEntity {
  eventoId: number
  tipoAlumnoId: number
  articuloCodigo?: string
  precioBase: number
  precioCuotas?: number
  cantidadCuotas: number
  permiteDescuento: boolean
  activo: boolean
}

export interface EventoProvinciaBeneficio extends BaseEntity {
  eventoId: number
  provinciaCodigo: string
  aplicaPrecioSocio: boolean
  porcentajeDescuento: number
  activo: boolean
}

export interface Promocion extends BaseEntity {
  nombre: string
  descripcion?: string
  tipoAlumnoId?: number
  cantidadCursosRequeridos: number
  periodoMeses: number
  tipoDescuento: string
  valor: number
  acumulable: boolean
  fechaVigenciaDesde: string
  fechaVigenciaHasta: string
  diasValidezCupon: number
  activo: boolean
}

export interface PromocionCupon extends BaseEntity {
  promocionId: number
  documento: string
  codigo: string
  tipoDescuento: string
  valor: number
  acumulable: boolean
  usado: boolean
  fechaUso?: string
  inscripcionDestinoId?: number
  fechaVencimiento?: string
}

export interface EventoArticuloRegalo extends BaseEntity {
  eventoId: number
  tipoAlumnoId: number
  articuloCodigo: string
  descripcionArticulo?: string
  cantidad: number
  condicionEspecial?: string
  activo: boolean
}

export interface Inscripcion extends BaseEntity {
  eventoId: number
  tipoAlumnoId: number
  nombre: string
  apellido: string
  email: string
  telefono?: string
  documento?: string
  provincia?: string
  precioBase: number
  descuentoAplicado: number
  precioFinal: number
  estado: string
  observaciones?: string
  fechaInscripcion: string
  fechaNacimiento?: string
  domicilio?: string
  codigoPostal?: string
  localidad?: string
  pais?: string
  celular?: string
  profesion?: string
  especialidad?: string
  institucion?: string
  sector?: string
  sincronizadoTango?: boolean
}

export interface Provincia {
  codProvin: string
  nombrePro: string
}

export interface Pago extends BaseEntity {
  inscripcionId: number
  medioPago: string
  estadoPago: string
  monto: number
  referenciaExterna?: string
  fechaPago?: string
  observaciones?: string
}

export interface BecaEvento extends BaseEntity {
  eventoId: number
  nombreCampana: string
  tipoDescuento: string
  valor: number
  cantidadTotalCodigos: number
  fechaVencimiento?: string
  acumulable: boolean
  activo: boolean
}

export interface BecaCodigo extends BaseEntity {
  becaEventoId: number
  codigo: string
  usado: boolean
  fechaUso?: string
  inscripcionId?: number
}

// Form types for DTOs
export interface TipoEventoForm {
  nombre: string
  activo: boolean
}

export interface TipoAlumnoForm {
  nombre: string
  activo: boolean
}

export interface EventoForm {
  tipoEventoId: number
  titulo: string
  descripcion?: string
  fechaInicio: string
  fechaFin: string
  fechaCierreInscripcion: string
  lugar?: string
  modalidad: string
  maxInscriptos?: number | null
  activo?: boolean
}

export interface EventoPrecioForm {
  eventoId: number
  tipoAlumnoId: number
  articuloCodigo?: string
  precioBase: number
  precioCuotas?: number | null
  cantidadCuotas: number
  permiteDescuento: boolean
  activo: boolean
}

export interface EventoProvinciaBeneficioForm {
  eventoId: number
  provinciaCodigo: string
  aplicaPrecioSocio: boolean
  porcentajeDescuento: number
  activo: boolean
}

export interface PromocionForm {
  nombre: string
  descripcion?: string
  tipoAlumnoId?: number | null
  cantidadCursosRequeridos: number
  periodoMeses: number
  tipoDescuento: string
  valor: number
  acumulable: boolean
  fechaVigenciaDesde: string
  fechaVigenciaHasta: string
  diasValidezCupon: number
  activo: boolean
}

export interface EventoArticuloRegaloForm {
  eventoId: number
  tipoAlumnoId: number
  articuloCodigo: string
  descripcionArticulo?: string
  cantidad: number
  condicionEspecial?: string
  activo: boolean
}

export interface InscripcionForm {
  eventoId: number
  tipoAlumnoId: number
  nombre: string
  apellido: string
  email: string
  telefono?: string
  documento: string
  provincia?: string
  codigoBeca?: string
  codigoCupon?: string
  fechaNacimiento?: string
  domicilio?: string
  codigoPostal?: string
  localidad?: string
  pais?: string
  celular?: string
  profesion?: string
  especialidad?: string
  institucion?: string
  sector?: string
  cuotas?: number
  modalidadPago?: string
}

export interface PagoForm {
  inscripcionId: number
  medioPago: string
  monto: number
  referenciaExterna?: string
  fechaPago?: string
  observaciones?: string
}

export interface Usuario extends BaseEntity {
  username: string
  nombreCompleto: string
  email?: string | null
  activo: boolean
}

export interface UsuarioCreateForm {
  username: string
  password: string
  nombreCompleto: string
  email?: string
  activo: boolean
}

export interface UsuarioUpdateForm {
  username: string
  nombreCompleto: string
  email?: string
  activo: boolean
}

export interface ResumenCuenta {
  id: number
  cuit: string
  tComp: string
  nComp: string
  fechaVto: string
  saldo: number
}

export interface SocioData {
  documento: string
  apellido: string
  nombre: string
  domicilio?: string
  codigoPostal?: string
  provincia?: string
}

export interface BecaEventoForm {
  eventoId: number
  nombreCampana: string
  tipoDescuento: string
  valor: number
  cantidadTotalCodigos: number
  fechaVencimiento?: string
  acumulable: boolean
  activo: boolean
}
