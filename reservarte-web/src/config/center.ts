/**
 * Datos de contacto del centro piloto (More Than Brows), tal como están en Figma
 * (Contacto, `387:56672`). Provisional: la API todavía no expone horario ni
 * redes, y la ficha de la organización no tiene dirección. La dirección saldrá
 * de la base de datos (`869fabu4m`, H-42); cuando la API dé estos datos, este
 * módulo desaparecerá.
 */
export interface CenterContact {
  /** Dirección para el mapa. Sin ella, la pantalla de Contacto no muestra mapa. */
  address?: string;
  schedule: { day: string; hours: string[] }[];
  phone: { display: string; href: string };
  instagram: { display: string; href: string };
}

export const centerContact: CenterContact = {
  address: 'Calle Bolonia, 4, Zaragoza (50008)',
  schedule: [{ day: 'Lunes a viernes:', hours: ['de 10:00 a 14:00', 'de 15:00 a 20:00'] }],
  phone: { display: '649 227 139', href: 'tel:+34649227139' },
  instagram: {
    display: '@morethanbrows.zgz',
    href: 'https://www.instagram.com/morethanbrows.zgz/',
  },
};

/**
 * Zona horaria del centro (RA-869d7fbyt). El horario y las citas van en hora local
 * del centro, sin zona; las ausencias de los empleados viajan en UTC y se pasan a
 * esta zona para mostrarlas y desde ella para guardarlas.
 */
export const CENTER_TIME_ZONE = 'Europe/Madrid';
