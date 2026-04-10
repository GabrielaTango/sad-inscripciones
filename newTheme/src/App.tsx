import { useState } from 'react';
import {
  Activity,
  Heart,
  Users,
  Calendar,
  Clock,
  Award,
  Phone,
  Mail,
  MapPin,
  ChevronDown,
  CheckCircle,
  BookOpen,
  Stethoscope,
  GraduationCap,
  FileText
} from 'lucide-react';

// Types
interface Course {
  id: number;
  title: string;
  description: string;
  duration: string;
  credits: number;
  category: string;
  nextDate: string;
  spots: number;
  featured?: boolean;
}

interface FormData {
  name: string;
  lastName: string;
  email: string;
  phone: string;
  professionalId: string;
  specialty: string;
  institution: string;
  selectedCourse: number | null;
  comments: string;
}

// Courses data
const courses: Course[] = [
  {
    id: 1,
    title: "Diplomado en Manejo Integral de Diabetes Mellitus",
    description: "Programa completo de formación en el manejo clínico de la diabetes, incluyendo farmacología, complicaciones y manejo interdisciplinario.",
    duration: "120 horas",
    credits: 24,
    category: "Diplomado",
    nextDate: "15 Mayo 2026",
    spots: 30,
    featured: true
  },
  {
    id: 2,
    title: "Workshop: Tecnología Aplicada al Control Glucémico",
    description: "Actualización en sistemas de monitoreo continuo de glucosa, bombas de insulina y tecnologías emergentes.",
    duration: "16 horas",
    credits: 4,
    category: "Workshop",
    nextDate: "22 Abril 2026",
    spots: 25
  },
  {
    id: 3,
    title: "Curso: Pie Diabético - Prevención y Tratamiento",
    description: "Enfoque práctico para la prevención, diagnóstico temprano y tratamiento de lesiones en pie diabético.",
    duration: "24 horas",
    credits: 6,
    category: "Curso",
    nextDate: "10 Junio 2026",
    spots: 20
  },
  {
    id: 4,
    title: "Seminario: Diabetes y Enfermedad Cardiovascular",
    description: "Actualización en la interrelación entre diabetes y complicaciones cardiovasculares, estrategias de prevención.",
    duration: "8 horas",
    credits: 2,
    category: "Seminario",
    nextDate: "5 Mayo 2026",
    spots: 50
  },
  {
    id: 5,
    title: "Diplomado: Educación Terapéutica en Diabetes",
    description: "Formación en técnicas de educación al paciente para mejorar la adherencia terapéutica y calidad de vida.",
    duration: "80 horas",
    credits: 18,
    category: "Diplomado",
    nextDate: "1 Julio 2026",
    spots: 25,
    featured: true
  },
  {
    id: 6,
    title: "Curso: Nutrición y Plan Alimentario en Diabetes",
    description: "Fundamentos de nutrición clínica aplicada a pacientes con diabetes, incluyendo conteo de carbohidratos.",
    duration: "20 horas",
    credits: 5,
    category: "Curso",
    nextDate: "18 Mayo 2026",
    spots: 30
  }
];

// Category colors
const categoryColors: Record<string, { bg: string; text: string }> = {
  Diplomado: { bg: 'bg-blue-100', text: 'text-blue-700' },
  Workshop: { bg: 'bg-purple-100', text: 'text-purple-700' },
  Curso: { bg: 'bg-green-100', text: 'text-green-700' },
  Seminario: { bg: 'bg-amber-100', text: 'text-amber-700' }
};

function App() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [selectedCourse, setSelectedCourse] = useState<number | null>(null);
  const [formSubmitted, setFormSubmitted] = useState(false);
  const [formData, setFormData] = useState<FormData>({
    name: '',
    lastName: '',
    email: '',
    phone: '',
    professionalId: '',
    specialty: '',
    institution: '',
    selectedCourse: null,
    comments: ''
  });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleCourseSelect = (courseId: number) => {
    setSelectedCourse(courseId);
    setFormData(prev => ({ ...prev, selectedCourse: courseId }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Simulate form submission
    setFormSubmitted(true);
    setTimeout(() => {
      setFormSubmitted(false);
      setFormData({
        name: '',
        lastName: '',
        email: '',
        phone: '',
        professionalId: '',
        specialty: '',
        institution: '',
        selectedCourse: null,
        comments: ''
      });
      setSelectedCourse(null);
    }, 3000);
  };

  const selectedCourseData = courses.find(c => c.id === selectedCourse);

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-slate-100">
      {/* Header */}
      <header className="bg-white shadow-sm sticky top-0 z-50">
        <nav className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16 md:h-20">
            {/* Logo */}
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 md:w-12 md:h-12 bg-gradient-to-br from-blue-600 to-blue-700 rounded-xl flex items-center justify-center">
                <Heart className="w-6 h-6 md:w-7 md:h-7 text-white" />
              </div>
              <div className="hidden sm:block">
                <h1 className="text-lg md:text-xl font-bold text-slate-800">Sociedad de Diabetes</h1>
                <p className="text-xs text-slate-500">Educación médica continua</p>
              </div>
            </div>

            {/* Desktop Navigation */}
            <div className="hidden md:flex items-center space-x-8">
              <a href="#cursos" className="text-slate-600 hover:text-blue-600 transition-colors font-medium">Cursos</a>
              <a href="#nosotros" className="text-slate-600 hover:text-blue-600 transition-colors font-medium">Nosotros</a>
              <a href="#inscripcion" className="bg-blue-600 text-white px-5 py-2 rounded-lg hover:bg-blue-700 transition-colors font-medium">
                Inscribirse
              </a>
            </div>

            {/* Mobile menu button */}
            <button
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              className="md:hidden p-2 rounded-lg hover:bg-slate-100"
            >
              <ChevronDown className={`w-6 h-6 text-slate-600 transition-transform ${isMenuOpen ? 'rotate-180' : ''}`} />
            </button>
          </div>

          {/* Mobile Navigation */}
          {isMenuOpen && (
            <div className="md:hidden py-4 border-t border-slate-100">
              <div className="flex flex-col space-y-3">
                <a href="#cursos" className="text-slate-600 hover:text-blue-600 transition-colors font-medium py-2">Cursos</a>
                <a href="#nosotros" className="text-slate-600 hover:text-blue-600 transition-colors font-medium py-2">Nosotros</a>
                <a href="#inscripcion" className="bg-blue-600 text-white px-5 py-2 rounded-lg hover:bg-blue-700 transition-colors font-medium text-center">
                  Inscribirse
                </a>
              </div>
            </div>
          )}
        </nav>
      </header>

      {/* Hero Section */}
      <section className="relative bg-gradient-to-br from-blue-600 via-blue-700 to-blue-800 text-white overflow-hidden">
        <div className="absolute inset-0 bg-grid-pattern opacity-10"></div>
        <div className="absolute top-20 right-10 w-64 h-64 bg-blue-400 rounded-full filter blur-3xl opacity-20"></div>
        <div className="absolute bottom-10 left-10 w-48 h-48 bg-teal-400 rounded-full filter blur-3xl opacity-20"></div>

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24 relative">
          <div className="grid md:grid-cols-2 gap-12 items-center">
            <div>
              <div className="inline-flex items-center space-x-2 bg-white/10 backdrop-blur-sm px-4 py-2 rounded-full mb-6">
                <Activity className="w-4 h-4" />
                <span className="text-sm font-medium">Certificación Internacional</span>
              </div>
              <h1 className="text-3xl sm:text-4xl md:text-5xl lg:text-6xl font-bold leading-tight mb-6">
                Formación Médica de Excelencia en Diabetes
              </h1>
              <p className="text-lg md:text-xl text-blue-100 mb-8 leading-relaxed">
                Únete a nuestra comunidad de profesionales de la salud y accede a programas educativos acreditados por expertos internacionales en endocrinología y metabolismo.
              </p>
              <div className="flex flex-col sm:flex-row gap-4">
                <a
                  href="#inscripcion"
                  className="bg-white text-blue-700 px-8 py-4 rounded-xl font-semibold hover:bg-blue-50 transition-colors text-center shadow-lg"
                >
                  Inscribirme Ahora
                </a>
                <a
                  href="#cursos"
                  className="border-2 border-white/30 text-white px-8 py-4 rounded-xl font-semibold hover:bg-white/10 transition-colors text-center"
                >
                  Ver Cursos
                </a>
              </div>
            </div>

            <div className="hidden md:grid grid-cols-2 gap-4">
              <div className="space-y-4">
                <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-6">
                  <Users className="w-10 h-10 mb-4 text-blue-200" />
                  <h3 className="text-2xl font-bold">2,500+</h3>
                  <p className="text-blue-100">Profesionales certificados</p>
                </div>
                <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-6">
                  <Award className="w-10 h-10 mb-4 text-blue-200" />
                  <h3 className="text-2xl font-bold">15+</h3>
                  <p className="text-blue-100">Años de experiencia</p>
                </div>
              </div>
              <div className="space-y-4 mt-8">
                <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-6">
                  <BookOpen className="w-10 h-10 mb-4 text-blue-200" />
                  <h3 className="text-2xl font-bold">50+</h3>
                  <p className="text-blue-100">Cursos disponibles</p>
                </div>
                <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-6">
                  <Stethoscope className="w-10 h-10 mb-4 text-blue-200" />
                  <h3 className="text-2xl font-bold">30+</h3>
                  <p className="text-blue-100">Expertos instructores</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* About Section */}
      <section id="nosotros" className="py-16 md:py-24 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <span className="text-blue-600 font-semibold text-sm uppercase tracking-wider">Sobre Nosotros</span>
            <h2 className="text-3xl md:text-4xl font-bold text-slate-800 mt-4 mb-6">
              Liderando la Educación en Diabetes
            </h2>
            <p className="text-slate-600 text-lg leading-relaxed">
              La Sociedad de Diabetes es una organización médica dedicada a la formación continua de profesionales de la salud, promoviendo las mejores prácticas clínicas y la investigación científica para mejorar la calidad de vida de los pacientes.
            </p>
          </div>

          <div className="grid md:grid-cols-3 gap-8">
            <div className="text-center p-8 rounded-2xl bg-gradient-to-b from-blue-50 to-white border border-blue-100">
              <div className="w-16 h-16 bg-blue-100 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <GraduationCap className="w-8 h-8 text-blue-600" />
              </div>
              <h3 className="text-xl font-bold text-slate-800 mb-4">Formación Acreditada</h3>
              <p className="text-slate-600">
                Todos nuestros programas están acreditados por colegios médicos y sociedades científicas internacionales.
              </p>
            </div>

            <div className="text-center p-8 rounded-2xl bg-gradient-to-b from-teal-50 to-white border border-teal-100">
              <div className="w-16 h-16 bg-teal-100 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <Users className="w-8 h-8 text-teal-600" />
              </div>
              <h3 className="text-xl font-bold text-slate-800 mb-4">Comunidad Activa</h3>
              <p className="text-slate-600">
                Conecta con colegas de toda Latinoamérica, comparte experiencias y expande tu red profesional.
              </p>
            </div>

            <div className="text-center p-8 rounded-2xl bg-gradient-to-b from-purple-50 to-white border border-purple-100">
              <div className="w-16 h-16 bg-purple-100 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <FileText className="w-8 h-8 text-purple-600" />
              </div>
              <h3 className="text-xl font-bold text-slate-800 mb-4">Recursos Exclusivos</h3>
              <p className="text-slate-600">
                Accede a materiales didácticos actualizados, estudios de caso y herramientas clínicas prácticas.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Courses Section */}
      <section id="cursos" className="py-16 md:py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <span className="text-blue-600 font-semibold text-sm uppercase tracking-wider">Oferta Académica</span>
            <h2 className="text-3xl md:text-4xl font-bold text-slate-800 mt-4 mb-6">
              Cursos y Programas 2026
            </h2>
            <p className="text-slate-600 text-lg">
              Selecciona el programa que mejor se adapte a tus necesidades de formación continua.
            </p>
          </div>

          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
            {courses.map((course) => (
              <div
                key={course.id}
                className={`bg-white rounded-2xl overflow-hidden shadow-sm hover:shadow-xl transition-all duration-300 border border-slate-200 ${
                  course.featured ? 'ring-2 ring-blue-500 ring-offset-2' : ''
                }`}
              >
                {course.featured && (
                  <div className="bg-blue-600 text-white text-center py-2 text-sm font-semibold">
                    ⭐ Curso Destacado
                  </div>
                )}
                <div className="p-6">
                  <div className="flex items-center justify-between mb-4">
                    <span className={`px-3 py-1 rounded-full text-xs font-semibold ${categoryColors[course.category].bg} ${categoryColors[course.category].text}`}>
                      {course.category}
                    </span>
                    <div className="flex items-center space-x-1 text-slate-500 text-sm">
                      <Clock className="w-4 h-4" />
                      <span>{course.duration}</span>
                    </div>
                  </div>

                  <h3 className="text-lg font-bold text-slate-800 mb-3 leading-tight">
                    {course.title}
                  </h3>

                  <p className="text-slate-600 text-sm mb-4 line-clamp-3">
                    {course.description}
                  </p>

                  <div className="flex items-center justify-between text-sm text-slate-500 mb-4">
                    <div className="flex items-center space-x-1">
                      <Calendar className="w-4 h-4" />
                      <span>{course.nextDate}</span>
                    </div>
                    <div className="flex items-center space-x-1">
                      <Award className="w-4 h-4" />
                      <span>{course.credits} créditos</span>
                    </div>
                  </div>

                  <div className="flex items-center justify-between">
                    <span className="text-sm text-slate-500">
                      {course.spots} cupos disponibles
                    </span>
                    <button
                      onClick={() => handleCourseSelect(course.id)}
                      className={`px-4 py-2 rounded-lg font-medium text-sm transition-all ${
                        selectedCourse === course.id
                          ? 'bg-green-600 text-white'
                          : 'bg-blue-600 text-white hover:bg-blue-700'
                      }`}
                    >
                      {selectedCourse === course.id ? (
                        <span className="flex items-center space-x-1">
                          <CheckCircle className="w-4 h-4" />
                          <span>Seleccionado</span>
                        </span>
                      ) : (
                        'Seleccionar'
                      )}
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Registration Form Section */}
      <section id="inscripcion" className="py-16 md:py-24 bg-gradient-to-br from-slate-800 to-slate-900">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid lg:grid-cols-2 gap-12 items-start">
            <div className="text-white">
              <span className="text-blue-400 font-semibold text-sm uppercase tracking-wider">Proceso de Inscripción</span>
              <h2 className="text-3xl md:text-4xl font-bold mt-4 mb-6">
                Inscríbete en tu Próximo Curso
              </h2>
              <p className="text-slate-300 text-lg mb-8 leading-relaxed">
                Completa el formulario con tus datos profesionales. Notre equipo se pondrá en contacto contigo en un plazo de 24-48 horas hábiles para confirmar tu inscripción y proporcionarte los detalles de acceso.
              </p>

              <div className="space-y-6">
                <div className="flex items-start space-x-4">
                  <div className="w-10 h-10 bg-blue-600 rounded-xl flex items-center justify-center flex-shrink-0">
                    <span className="text-white font-bold">1</span>
                  </div>
                  <div>
                    <h4 className="font-semibold mb-1">Selecciona tu curso</h4>
                    <p className="text-slate-400 text-sm">Elige el programa de tu interés desde la lista de cursos disponibles.</p>
                  </div>
                </div>

                <div className="flex items-start space-x-4">
                  <div className="w-10 h-10 bg-blue-600 rounded-xl flex items-center justify-center flex-shrink-0">
                    <span className="text-white font-bold">2</span>
                  </div>
                  <div>
                    <h4 className="font-semibold mb-1">Completa tus datos</h4>
                    <p className="text-slate-400 text-sm">Ingresa tu información profesional para la generación de certificados.</p>
                  </div>
                </div>

                <div className="flex items-start space-x-4">
                  <div className="w-10 h-10 bg-blue-600 rounded-xl flex items-center justify-center flex-shrink-0">
                    <span className="text-white font-bold">3</span>
                  </div>
                  <div>
                    <h4 className="font-semibold mb-1">Confirma tu inscripción</h4>
                    <p className="text-slate-400 text-sm">Recibirás un correo de confirmación con las instrucciones de pago y acceso.</p>
                  </div>
                </div>
              </div>

              {/* Selected Course Preview */}
              {selectedCourseData && (
                <div className="mt-8 bg-white/5 backdrop-blur-sm rounded-2xl p-6 border border-white/10">
                  <h4 className="text-sm font-semibold text-blue-400 uppercase tracking-wider mb-3">Curso Seleccionado</h4>
                  <h3 className="text-xl font-bold text-white mb-2">{selectedCourseData.title}</h3>
                  <div className="flex flex-wrap gap-4 text-sm text-slate-300">
                    <span className="flex items-center space-x-1">
                      <Clock className="w-4 h-4" />
                      <span>{selectedCourseData.duration}</span>
                    </span>
                    <span className="flex items-center space-x-1">
                      <Award className="w-4 h-4" />
                      <span>{selectedCourseData.credits} créditos</span>
                    </span>
                    <span className="flex items-center space-x-1">
                      <Calendar className="w-4 h-4" />
                      <span>{selectedCourseData.nextDate}</span>
                    </span>
                  </div>
                </div>
              )}
            </div>

            {/* Form */}
            <div className="bg-white rounded-2xl p-6 md:p-8 shadow-2xl">
              {formSubmitted ? (
                <div className="text-center py-12">
                  <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
                    <CheckCircle className="w-10 h-10 text-green-600" />
                  </div>
                  <h3 className="text-2xl font-bold text-slate-800 mb-4">¡Inscripción Enviada!</h3>
                  <p className="text-slate-600">
                    Hemos recibido tu solicitud. Te contactaremos pronto para completar el proceso de inscripción.
                  </p>
                </div>
              ) : (
                <form onSubmit={handleSubmit} className="space-y-6">
                  <div className="grid sm:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-2">
                        Nombre *
                      </label>
                      <input
                        type="text"
                        name="name"
                        value={formData.name}
                        onChange={handleInputChange}
                        required
                        className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                        placeholder="María"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-2">
                        Apellido *
                      </label>
                      <input
                        type="text"
                        name="lastName"
                        value={formData.lastName}
                        onChange={handleInputChange}
                        required
                        className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                        placeholder="García López"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      Correo Electrónico *
                    </label>
                    <input
                      type="email"
                      name="email"
                      value={formData.email}
                      onChange={handleInputChange}
                      required
                      className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                      placeholder="maria.garcia@hospital.com"
                    />
                  </div>

                  <div className="grid sm:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-2">
                        Teléfono *
                      </label>
                      <input
                        type="tel"
                        name="phone"
                        value={formData.phone}
                        onChange={handleInputChange}
                        required
                        className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                        placeholder="+52 55 1234 5678"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-2">
                        Cédula Profesional *
                      </label>
                      <input
                        type="text"
                        name="professionalId"
                        value={formData.professionalId}
                        onChange={handleInputChange}
                        required
                        className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                        placeholder="12345678"
                      />
                    </div>
                  </div>

                  <div className="grid sm:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-2">
                        Especialidad *
                      </label>
                      <select
                        name="specialty"
                        value={formData.specialty}
                        onChange={handleInputChange}
                        required
                        className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all bg-white"
                      >
                        <option value="">Seleccionar...</option>
                        <option value="medico-general">Médico General</option>
                        <option value="endocrinologia">Endocrinología</option>
                        <option value="medicina-interna">Medicina Interna</option>
                        <option value="cardiologia">Cardiología</option>
                        <option value="nutriologia">Nutriología</option>
                        <option value="enfermeria">Enfermería</option>
                        <option value="podologia">Podología</option>
                        <option value="educacion">Educación en Diabetes</option>
                        <option value="otro">Otro</option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-2">
                        Institución *
                      </label>
                      <input
                        type="text"
                        name="institution"
                        value={formData.institution}
                        onChange={handleInputChange}
                        required
                        className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                        placeholder="Hospital Central"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      Curso de Interés *
                    </label>
                    <select
                      name="selectedCourse"
                      value={formData.selectedCourse || ''}
                      onChange={(e) => handleCourseSelect(Number(e.target.value))}
                      required
                      className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all bg-white"
                    >
                      <option value="">Seleccionar un curso...</option>
                      {courses.map((course) => (
                        <option key={course.id} value={course.id}>
                          {course.title}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      Comentarios Adicionales
                    </label>
                    <textarea
                      name="comments"
                      value={formData.comments}
                      onChange={handleInputChange}
                      rows={3}
                      className="w-full px-4 py-3 rounded-xl border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all resize-none"
                      placeholder="¿Alguna duda o comentario sobre el curso?"
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={!formData.selectedCourse}
                    className={`w-full py-4 rounded-xl font-semibold text-lg transition-all ${
                      formData.selectedCourse
                        ? 'bg-blue-600 text-white hover:bg-blue-700 shadow-lg hover:shadow-xl'
                        : 'bg-slate-300 text-slate-500 cursor-not-allowed'
                    }`}
                  >
                    {formData.selectedCourse ? 'Enviar Inscripción' : 'Selecciona un curso para continuar'}
                  </button>

                  <p className="text-xs text-slate-500 text-center">
                    Al enviar este formulario, aceptas nuestra política de privacidad y términos de servicio.
                  </p>
                </form>
              )}
            </div>
          </div>
        </div>
      </section>

      {/* Contact Section */}
      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-3 gap-8">
            <div className="flex items-center space-x-4 p-6 rounded-2xl bg-slate-50">
              <div className="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center flex-shrink-0">
                <Phone className="w-6 h-6 text-blue-600" />
              </div>
              <div>
                <h4 className="font-semibold text-slate-800">Teléfono</h4>
                <p className="text-slate-600">+52 55 5555 5555</p>
              </div>
            </div>

            <div className="flex items-center space-x-4 p-6 rounded-2xl bg-slate-50">
              <div className="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center flex-shrink-0">
                <Mail className="w-6 h-6 text-blue-600" />
              </div>
              <div>
                <h4 className="font-semibold text-slate-800">Correo</h4>
                <p className="text-slate-600">educacion@sociedaddiabetes.org</p>
              </div>
            </div>

            <div className="flex items-center space-x-4 p-6 rounded-2xl bg-slate-50">
              <div className="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center flex-shrink-0">
                <MapPin className="w-6 h-6 text-blue-600" />
              </div>
              <div>
                <h4 className="font-semibold text-slate-800">Ubicación</h4>
                <p className="text-slate-600">Ciudad de México, México</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-slate-900 text-white py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex flex-col md:flex-row justify-between items-center">
            <div className="flex items-center space-x-3 mb-6 md:mb-0">
              <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-blue-600 rounded-xl flex items-center justify-center">
                <Heart className="w-5 h-5 text-white" />
              </div>
              <div>
                <h3 className="font-bold">Sociedad de Diabetes</h3>
                <p className="text-slate-400 text-sm">Educación médica continua</p>
              </div>
            </div>

            <div className="flex flex-wrap justify-center gap-6 text-sm text-slate-400">
              <a href="#" className="hover:text-white transition-colors">Términos y Condiciones</a>
              <a href="#" className="hover:text-white transition-colors">Política de Privacidad</a>
              <a href="#" className="hover:text-white transition-colors">Contacto</a>
            </div>
          </div>

          <div className="border-t border-slate-800 mt-8 pt-8 text-center text-sm text-slate-500">
            <p>© 2026 Sociedad de Diabetes. Todos los derechos reservados.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}

export default App;
