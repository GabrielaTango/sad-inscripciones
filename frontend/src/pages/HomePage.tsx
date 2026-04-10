import { Link } from 'react-router-dom'
import { GraduationCap, BookOpen, Users, Calendar, MapPin, Laptop, ArrowRight, UserPlus } from 'lucide-react'
import Hero from '../components/Hero/Hero'

const HomePage = () => {
  return (
    <>
      <Hero />

      {/* Servicios destacados */}
      <section className="py-16 md:py-24 bg-white">
        <div className="max-w-7xl mx-auto px-4">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <span className="section-label">NUESTROS SERVICIOS</span>
            <h2 className="section-title">¿Qué hacemos?</h2>
            <p className="section-subtitle">
              Trabajamos para mejorar la calidad de vida de las personas con diabetes
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div className="bg-gradient-to-b from-blue-50 to-white rounded-2xl border border-blue-100 hover:shadow-xl transition-all duration-300 h-full text-center p-4">
              <div className="p-6">
                <div
                  className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-3"
                >
                  <GraduationCap className="w-8 h-8 text-blue-600" />
                </div>
                <h5 className="text-lg font-bold text-slate-800">Formación Continua</h5>
                <p className="text-slate-600">
                  Cursos, talleres y programas de actualización para profesionales
                  de la salud especializados en diabetes.
                </p>
              </div>
            </div>

            <div className="bg-gradient-to-b from-blue-50 to-white rounded-2xl border border-blue-100 hover:shadow-xl transition-all duration-300 h-full text-center p-4">
              <div className="p-6">
                <div
                  className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-3"
                >
                  <BookOpen className="w-8 h-8 text-blue-600" />
                </div>
                <h5 className="text-lg font-bold text-slate-800">Investigación</h5>
                <p className="text-slate-600">
                  Promovemos y difundimos investigaciones científicas sobre
                  prevención, diagnóstico y tratamiento de la diabetes.
                </p>
              </div>
            </div>

            <div className="bg-gradient-to-b from-blue-50 to-white rounded-2xl border border-blue-100 hover:shadow-xl transition-all duration-300 h-full text-center p-4">
              <div className="p-6">
                <div
                  className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-3"
                >
                  <Users className="w-8 h-8 text-blue-600" />
                </div>
                <h5 className="text-lg font-bold text-slate-800">Comunidad</h5>
                <p className="text-slate-600">
                  Red de profesionales comprometidos con la excelencia en el
                  cuidado de pacientes con diabetes.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Próximos eventos */}
      <section className="py-16 md:py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-4">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <span className="section-label">CALENDARIO</span>
            <h2 className="section-title">Próximos Eventos</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 hover:shadow-xl transition-all duration-300 h-full">
              <div className="p-6">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-600 text-white mb-2">Congreso</span>
                <h5 className="text-lg font-bold text-slate-800">Congreso Anual SAD 2026</h5>
                <p className="text-slate-600 text-sm">
                  <Calendar className="w-4 h-4 inline-block mr-1" /> 15-17 de Mayo, 2026
                </p>
                <p className="text-slate-600 text-sm">
                  <MapPin className="w-4 h-4 inline-block mr-1" /> Buenos Aires, Argentina
                </p>
                <p className="text-slate-600">
                  Encuentro de especialistas con las últimas novedades en
                  tratamiento y tecnología aplicada a la diabetes.
                </p>
              </div>
            </div>

            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 hover:shadow-xl transition-all duration-300 h-full">
              <div className="p-6">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-600 text-white mb-2">Curso</span>
                <h5 className="text-lg font-bold text-slate-800">Curso de Insulinoterapia</h5>
                <p className="text-slate-600 text-sm">
                  <Calendar className="w-4 h-4 inline-block mr-1" /> 10 de Marzo, 2026
                </p>
                <p className="text-slate-600 text-sm">
                  <Laptop className="w-4 h-4 inline-block mr-1" /> Virtual
                </p>
                <p className="text-slate-600">
                  Actualización en esquemas de insulinoterapia para médicos
                  clínicos y endocrinólogos.
                </p>
              </div>
            </div>

            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 hover:shadow-xl transition-all duration-300 h-full">
              <div className="p-6">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-600 text-white mb-2">Taller</span>
                <h5 className="text-lg font-bold text-slate-800">Taller de Pie Diabético</h5>
                <p className="text-slate-600 text-sm">
                  <Calendar className="w-4 h-4 inline-block mr-1" /> 22 de Abril, 2026
                </p>
                <p className="text-slate-600 text-sm">
                  <MapPin className="w-4 h-4 inline-block mr-1" /> Córdoba, Argentina
                </p>
                <p className="text-slate-600">
                  Taller práctico sobre prevención, diagnóstico y manejo del
                  pie diabético.
                </p>
              </div>
            </div>
          </div>

          <div className="text-center mt-4">
            <Link to="/eventos" className="btn-primary px-4">
              Ver todos los eventos <ArrowRight className="w-4 h-4 inline-block ml-1" />
            </Link>
          </div>
        </div>
      </section>

      {/* CTA Inscripción */}
      <section className="py-16 md:py-24 text-white text-center bg-gradient-to-br from-slate-800 to-slate-900">
        <div className="max-w-7xl mx-auto px-4">
          <span className="text-blue-400 text-sm font-semibold tracking-wider uppercase mb-4 block">SUMATE</span>
          <h2 className="font-bold mb-3">Unite a la Sociedad Argentina de Diabetes</h2>
          <p className="text-lg text-slate-400 mb-4">
            Formá parte de la comunidad de profesionales que trabaja por mejorar
            la calidad de vida de las personas con diabetes.
          </p>
          <Link to="/inscripcion" className="btn-accent btn-lg px-6">
            <UserPlus className="w-5 h-5 inline-block mr-2" />Inscribirse ahora
          </Link>
        </div>
      </section>
    </>
  )
}

export default HomePage
