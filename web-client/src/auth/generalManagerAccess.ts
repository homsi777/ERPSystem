export const GENERAL_MANAGER_PERMISSION = 'security.general-manager';

export function isGeneralManager(permissions: readonly string[]): boolean {
  return permissions.some((code) => code.toLowerCase() === GENERAL_MANAGER_PERMISSION);
}

export function canViewSensitivePricing(permissions: readonly string[]): boolean {
  return isGeneralManager(permissions);
}
