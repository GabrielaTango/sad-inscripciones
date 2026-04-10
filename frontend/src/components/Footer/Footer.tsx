import { Link } from 'react-router-dom'
import { Heart } from 'lucide-react'

const Footer = () => {
  return (
    <footer className="bg-slate-900 text-white py-12">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col md:flex-row justify-between items-center">
          <Link to="/" className="flex items-center space-x-3 mb-6 md:mb-0">
            <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-blue-600 rounded-xl flex items-center justify-center">
              <Heart className="w-5 h-5 text-white" />
            </div>
            <div>
              <h3 className="font-bold">Sociedad de Diabetes</h3>
              <p className="text-slate-400 text-sm">Educación médica continua</p>
            </div>
          </Link>

          <div className="flex flex-wrap justify-center gap-6 text-sm text-slate-400">
            <Link to="/nosotros" className="hover:text-white transition-colors">Nosotros</Link>
            <Link to="/eventos" className="hover:text-white transition-colors">Eventos</Link>
            <Link to="/contacto" className="hover:text-white transition-colors">Contacto</Link>
            <a href="#" className="hover:text-white transition-colors">Términos y Condiciones</a>
            <a href="#" className="hover:text-white transition-colors">Política de Privacidad</a>
          </div>
        </div>

        <div className="border-t border-slate-800 mt-8 pt-8 text-center text-sm text-slate-500">
          <p>&copy; {new Date().getFullYear()} Sociedad Argentina de Diabetes. Todos los derechos reservados.</p>
        </div>
      </div>
    </footer>
  )
}

export default Footer
