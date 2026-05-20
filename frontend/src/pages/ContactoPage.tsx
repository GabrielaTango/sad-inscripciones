import { useState } from 'react'
import { MapPin, Mail, Phone, Clock, CheckCircle, Send } from 'lucide-react'

const ContactoPage = () => {
  const [form, setForm] = useState({ nombre: '', email: '', asunto: '', mensaje: '' })
  const [enviado, setEnviado] = useState(false)

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    setForm({ ...form, [e.target.name]: e.target.value })
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    console.log('Contacto:', form)
    setEnviado(true)
  }

  return (
    <>
      <section className="page-header">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="font-bold text-3xl">Contacto</h1>
          <p className="text-lg text-white/90 mb-0">
            Estamos para ayudarte. Envianos tu consulta.
          </p>
        </div>
      </section>

      <section className="py-16 md:py-24">
        <div className="max-w-7xl mx-auto px-4">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-10">
            {/* Info de contacto */}
            <div className="lg:col-span-1">
              <h4 className="font-bold text-slate-800 mb-4 text-xl">Información de Contacto</h4>

              <div className="flex items-center space-x-4 p-6 rounded-2xl bg-slate-50 mb-4">
                <div className="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
                  <MapPin className="text-blue-600 w-5 h-5" />
                </div>
                <div>
                  <h6 className="font-bold mb-1 text-base text-slate-800">Dirección</h6>
                  <p className="text-slate-600 text-sm mb-0">Paraguay 1307 8vo 74 CABA, Buenos Aires, Argentina</p>
                </div>
              </div>

              <div className="flex items-center space-x-4 p-6 rounded-2xl bg-slate-50 mb-4">
                <div className="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
                  <Mail className="text-blue-600 w-5 h-5" />
                </div>
                <div>
                  <h6 className="font-bold mb-1 text-base text-slate-800">Email</h6>
                  <p className="text-slate-600 text-sm mb-0">sad@diabetes.org.ar</p>
                </div>
              </div>

              <div className="flex items-center space-x-4 p-6 rounded-2xl bg-slate-50 mb-4">
                <div className="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
                  <Phone className="text-blue-600 w-5 h-5" />
                </div>
                <div>
                  <h6 className="font-bold mb-1 text-base text-slate-800">Teléfono</h6>
                  <p className="text-slate-600 text-sm mb-0">11 4813-4269</p>
                </div>
              </div>

              <div className="flex items-center space-x-4 p-6 rounded-2xl bg-slate-50 mb-4">
                <div className="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
                  <Clock className="text-blue-600 w-5 h-5" />
                </div>
                <div>
                  <h6 className="font-bold mb-1 text-base text-slate-800">Horario de Atención</h6>
                  <p className="text-slate-600 text-sm mb-0">Lunes a Viernes 9:00 - 17:00</p>
                </div>
              </div>
            </div>

            {/* Formulario */}
            <div className="lg:col-span-2">
              {enviado ? (
                <div className="text-center py-16">
                  <CheckCircle className="w-16 h-16 text-green-500 mx-auto" />
                  <h3 className="font-bold mt-3 text-2xl text-slate-800">¡Mensaje enviado!</h3>
                  <p className="text-slate-600">Te responderemos a la brevedad.</p>
                  <button className="btn-primary" onClick={() => setEnviado(false)}>
                    Enviar otro mensaje
                  </button>
                </div>
              ) : (
                <div className="bg-white rounded-2xl border border-slate-200 shadow-sm">
                  <div className="p-6">
                    <h4 className="font-bold text-slate-800 mb-4 text-xl">Envianos tu consulta</h4>
                    <form onSubmit={handleSubmit}>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div>
                          <label className="form-label">Nombre *</label>
                          <input
                            type="text"
                            className="form-input"
                            name="nombre"
                            value={form.nombre}
                            onChange={handleChange}
                            required
                          />
                        </div>
                        <div>
                          <label className="form-label">Email *</label>
                          <input
                            type="email"
                            className="form-input"
                            name="email"
                            value={form.email}
                            onChange={handleChange}
                            required
                          />
                        </div>
                        <div className="md:col-span-2">
                          <label className="form-label">Asunto *</label>
                          <select
                            className="form-select"
                            name="asunto"
                            value={form.asunto}
                            onChange={handleChange}
                            required
                          >
                            <option value="">Seleccionar...</option>
                            <option value="inscripcion">Consulta sobre inscripción</option>
                            <option value="eventos">Consulta sobre eventos</option>
                            <option value="cursos">Consulta sobre cursos</option>
                            <option value="general">Consulta general</option>
                          </select>
                        </div>
                        <div className="md:col-span-2">
                          <label className="form-label">Mensaje *</label>
                          <textarea
                            className="form-input"
                            name="mensaje"
                            rows={5}
                            value={form.mensaje}
                            onChange={handleChange}
                            required
                          ></textarea>
                        </div>
                        <div className="md:col-span-2">
                          <button type="submit" className="btn-primary btn-lg">
                            <Send className="w-4 h-4 mr-2 inline" />Enviar Mensaje
                          </button>
                        </div>
                      </div>
                    </form>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </section>
    </>
  )
}

export default ContactoPage
