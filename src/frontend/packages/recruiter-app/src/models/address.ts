export interface Address {
  streetNumber?: string;
  streetName?: string;
  city?: string;
  postalCode?: string;
  region?: string;
  country?: string;
  addressLine2?: string;
}

export interface AddressDto {
  streetNumber?: string;
  streetName?: string;
  city?: string;
  postalCode?: string;
  region?: string;
  country?: string;
  addressLine2?: string;
}

export interface CreateAddressDto {
  streetNumber?: string;
  streetName?: string;
  city?: string;
  postalCode?: string;
  region?: string;
  country?: string;
  addressLine2?: string;
}

export interface UpdateAddressDto {
  streetNumber?: string;
  streetName?: string;
  city?: string;
  postalCode?: string;
  region?: string;
  country?: string;
  addressLine2?: string;
}
