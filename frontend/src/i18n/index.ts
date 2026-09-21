import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import uk from './locales/uk.json'
import en from './locales/en.json'

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      uk: { translation: uk },
      en: { translation: en },
    },
    lng: undefined,
    fallbackLng: 'uk',
    supportedLngs: ['uk', 'en'],
    interpolation: { escapeValue: false },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
      lookupLocalStorage: 'crm.locale',
    },
  })

export default i18n
