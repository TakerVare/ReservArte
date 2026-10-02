/** Categorías de clienta (`CustomerCategories`). */
export const CUSTOMER_CATEGORIES = ['new', 'regular', 'vip'] as const;
export type CustomerCategory = (typeof CUSTOMER_CATEGORIES)[number];

/** Canales de contacto (`CustomerContactMethods`). */
export const CONTACT_METHODS = ['email', 'phone', 'sms', 'whatsapp'] as const;
export type ContactMethod = (typeof CONTACT_METHODS)[number];

/**
 * Consentimientos que se recogen en el alta (`CustomerConsentTypes`). `saved_cards`
 * queda fuera del piloto: no hay tarjeta guardada hasta la Fase 7 (`869f2gnbm`).
 */
export const CONSENT_TYPES = ['data_processing', 'marketing', 'photos', 'whatsapp'] as const;
export type ConsentType = (typeof CONSENT_TYPES)[number] | 'saved_cards';
/** El que exige la API (`CustomerConsentTypes.Required`). */
export const REQUIRED_CONSENTS: ConsentType[] = ['data_processing'];

/** Espejo de `CustomerDto`. */
export interface Customer {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone?: string | null;
  profileImageUrl?: string | null;
  /** `yyyy-MM-dd`. */
  birthDate?: string | null;
  category: CustomerCategory;
  loyaltyPoints: number;
  isBlocked: boolean;
  blockedReason?: string | null;
  preferredContactMethod: ContactMethod;
  /** UTC. */
  lastAllergyTestAt?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CustomerConsent {
  consentType: ConsentType;
  isGranted: boolean;
  grantedAt?: string | null;
  revokedAt?: string | null;
}

export interface CustomerAllergy {
  id: number;
  allergyDescription: string;
  severity: 'low' | 'medium' | 'high';
}

export interface CustomerNote {
  id: number;
  note: string;
  employeeId: number;
  employeeName?: string | null;
  createdAt: string;
}

/** Espejo de `CustomerDetailDto`. */
export interface CustomerDetail extends Customer {
  consents: CustomerConsent[];
  allergies: CustomerAllergy[];
  notes: CustomerNote[];
}

/** Cuerpo de la edición (`UpdateCustomerRequest`). */
export interface CustomerInput {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  birthDate?: string | null;
  profileImageUrl?: string | null;
  category: CustomerCategory;
  preferredContactMethod: ContactMethod;
}

/** Cuerpo del alta (`CreateCustomerRequest`). */
export interface CreateCustomerInput extends CustomerInput {
  grantedConsents: ConsentType[];
}
