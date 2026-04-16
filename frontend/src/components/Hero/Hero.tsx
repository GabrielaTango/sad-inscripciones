import { Link } from 'react-router-dom'
import { Users, Award, BookOpen, GraduationCap } from 'lucide-react'
import logo from '../../assets/circulo.png'

const Hero = () => {
  return (
    <section className="relative bg-gradient-to-br from-blue-600 via-blue-700 to-blue-800 text-white overflow-hidden">
      <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle at 1px 1px, white 1px, transparent 0)', backgroundSize: '40px 40px' }}></div>
      <div className="absolute top-20 right-10 w-64 h-64 bg-blue-400 rounded-full blur-3xl opacity-20"></div>
      <div className="absolute bottom-10 left-10 w-48 h-48 bg-teal-400 rounded-full blur-3xl opacity-20"></div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24 relative">
        <div className="grid md:grid-cols-2 gap-12 items-center">
          <div>
            <div className="inline-flex items-center space-x-2 bg-white text-blue-700 backdrop-blur-sm px-4 py-2 rounded-full mb-6">
              <img src={logo} alt="SAD Logo" className="h-6 w-6" />
              <span className="text-sm font-medium">Sociedad Argentina de Diabetes</span>
            </div>
            <h1 className="text-3xl sm:text-4xl md:text-5xl lg:text-6xl font-bold leading-tight mb-6">
              Formación Médica de Excelencia en Diabetes
            </h1>
            <p className="text-lg md:text-xl text-blue-100 mb-8 leading-relaxed">
              Promovemos la excelencia en la prevención, diagnóstico y tratamiento
              de la diabetes a través de la formación continua, la investigación
              y el compromiso con la salud de nuestra comunidad.
            </p>
            <div className="flex flex-col sm:flex-row gap-4">
              <Link
                to="/eventos"
                className="bg-white text-blue-700 px-8 py-4 rounded-xl font-semibold hover:bg-blue-50 transition-colors text-center shadow-lg"
              >
                Inscribirme Ahora
              </Link>
              <Link
                to="/nosotros"
                className="border-2 border-white/30 text-white px-8 py-4 rounded-xl font-semibold hover:bg-white/10 transition-colors text-center"
              >
                Conocer más
              </Link>
            </div>
          </div>

          <div className="hidden md:block">
            <div className="bg-white rounded-3xl p-6 shadow-lg">
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-blue-50 rounded-2xl p-6">
                  <Users className="w-10 h-10 mb-4 text-blue-600" />
                  <h3 className="text-2xl font-bold text-slate-800">2,500+</h3>
                  <p className="text-slate-500">Profesionales certificados</p>
                </div>
                <div className="bg-blue-50 rounded-2xl p-6">
                  <Award className="w-10 h-10 mb-4 text-blue-600" />
                  <h3 className="text-2xl font-bold text-slate-800">15+</h3>
                  <p className="text-slate-500">Años de experiencia</p>
                </div>
                <div className="bg-blue-50 rounded-2xl p-6">
                  <BookOpen className="w-10 h-10 mb-4 text-blue-600" />
                  <h3 className="text-2xl font-bold text-slate-800">50+</h3>
                  <p className="text-slate-500">Cursos disponibles</p>
                </div>
                <div className="bg-blue-50 rounded-2xl p-6">
                  <GraduationCap className="w-10 h-10 mb-4 text-blue-600" />
                  <h3 className="text-2xl font-bold text-slate-800">30+</h3>
                  <p className="text-slate-500">Instructores expertos</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

export default Hero
