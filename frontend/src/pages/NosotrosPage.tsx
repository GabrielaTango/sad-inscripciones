import { Target, Eye, Award, ShieldCheck, Lightbulb, ThumbsUp, Building, type LucideIcon } from 'lucide-react'

const valores: { icon: LucideIcon; title: string; desc: string }[] = [
  { icon: Award, title: 'Excelencia', desc: 'Buscamos los más altos estándares en formación e investigación médica.' },
  { icon: ShieldCheck, title: 'Ética', desc: 'Actuamos con integridad y responsabilidad en todas nuestras actividades.' },
  { icon: Lightbulb, title: 'Innovación', desc: 'Incorporamos los últimos avances científicos y tecnológicos.' },
  { icon: ThumbsUp, title: 'Compromiso', desc: 'Dedicados a mejorar la calidad de vida de los pacientes con diabetes.' },
]

const NosotrosPage = () => {
  return (
    <>
      {/* Header */}
      <section className="page-header">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="font-bold text-4xl">Sobre Nosotros</h1>
          <p className="text-lg text-white/90 mb-0">
            Conocé nuestra historia, misión y el equipo que conforma la SAD
          </p>
        </div>
      </section>

      {/* Misión y Visión */}
      <section className="py-16 md:py-24 bg-white">
        <div className="max-w-7xl mx-auto px-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
            <div>
              <h3 className="font-bold text-slate-800 mb-3 flex items-center gap-2">
                <div className="w-10 h-10 bg-blue-100 rounded-xl flex items-center justify-center">
                  <Target className="w-5 h-5 text-blue-600" />
                </div>
                Nuestra Misión
              </h3>
              <p className="text-slate-600">
                Promover la excelencia en el cuidado de las personas con diabetes
                mediante la formación continua de profesionales de la salud, la
                investigación científica y la difusión de conocimientos actualizados
                sobre prevención, diagnóstico y tratamiento.
              </p>
            </div>
            <div>
              <h3 className="font-bold text-slate-800 mb-3 flex items-center gap-2">
                <div className="w-10 h-10 bg-blue-100 rounded-xl flex items-center justify-center">
                  <Eye className="w-5 h-5 text-blue-600" />
                </div>
                Nuestra Visión
              </h3>
              <p className="text-slate-600">
                Ser la institución de referencia en Argentina y Latinoamérica en
                diabetología, liderando la formación médica especializada y
                contribuyendo a la mejora de las políticas públicas de salud
                relacionadas con la diabetes.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Valores */}
      <section className="py-16 md:py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-4">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <span className="section-label">LO QUE NOS DEFINE</span>
            <h2 className="section-title">Nuestros Valores</h2>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {valores.map((valor, index) => (
              <div
                className="bg-gradient-to-b from-blue-50 to-white rounded-2xl border border-blue-100 hover:shadow-xl transition-all duration-300 text-center p-6 h-full"
                key={index}
              >
                <div className="p-6">
                  <div className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-3">
                    <valor.icon className="w-8 h-8 text-blue-600" />
                  </div>
                  <h5 className="font-bold text-slate-800">{valor.title}</h5>
                  <p className="text-slate-600 text-sm">{valor.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Historia */}
      <section className="py-16 md:py-24 bg-white">
        <div className="max-w-7xl mx-auto px-4">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-10 items-center">
            <div>
              <span className="section-label">NUESTRA TRAYECTORIA</span>
              <h2 className="font-bold text-slate-800 mb-4 text-3xl">Nuestra Historia</h2>
              <p className="text-slate-600">
                La Sociedad Argentina de Diabetes fue fundada con el objetivo de
                reunir a los profesionales de la salud dedicados al estudio y
                tratamiento de la diabetes en Argentina.
              </p>
              <p className="text-slate-600">
                A lo largo de los años, hemos organizado congresos nacionales e
                internacionales, publicado guías clínicas de referencia y formado
                a miles de profesionales en las últimas técnicas y tratamientos
                para la diabetes.
              </p>
              <p className="text-slate-600">
                Hoy, la SAD continúa siendo un pilar fundamental en la
                diabetología argentina, con presencia activa en la comunidad
                científica internacional.
              </p>
            </div>
            <div className="text-center">
              <div className="rounded-2xl inline-flex items-center justify-center bg-slate-50 w-full h-[300px]">
                <div className="text-center">
                  <div className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-2">
                    <Building className="w-8 h-8 text-blue-600" />
                  </div>
                  <span className="text-slate-500 block">Imagen institucional</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    </>
  )
}

export default NosotrosPage
