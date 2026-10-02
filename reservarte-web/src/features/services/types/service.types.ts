/** Espejo de `ServiceDto`. */
export interface Service {
  id: number;
  name: string;
  description?: string | null;
  durationMinutes: number;
  basePrice: number;
  categoryId?: number | null;
  categoryName?: string | null;
  imageUrl?: string | null;
  requiresAllergyTest: boolean;
  allergyTestHoursBefore: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Variación de un servicio: ajusta el precio y la duración del base. */
export interface ServiceVariation {
  id: number;
  name: string;
  priceModifier: number;
  durationModifier: number;
}

/** Espejo de `ServiceDetailDto`. Las tarifas por nivel no se aplican en el piloto. */
export interface ServiceDetail extends Service {
  variations: ServiceVariation[];
  pricings: { id: number; employeeLevel: string; price: number }[];
}

export interface ServiceCategory {
  id: number;
  name: string;
  description?: string | null;
  color?: string | null;
  displayOrder: number;
  isActive: boolean;
}

/** Cuerpo del alta y la edición (`Create/UpdateServiceRequest`). */
export interface ServiceInput {
  name: string;
  description?: string | null;
  durationMinutes: number;
  basePrice: number;
  categoryId?: number | null;
  imageUrl?: string | null;
  requiresAllergyTest: boolean;
  allergyTestHoursBefore: number;
}

export type VariationInput = Pick<ServiceVariation, 'name' | 'priceModifier' | 'durationModifier'>;
