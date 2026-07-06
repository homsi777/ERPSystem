export const inventoryStatusLabels = {
  available: 'متاح',
  low: 'منخفض'
} as const;

export const chinaContainerStatusLabels = {
  0: 'مسودة',
  1: 'قيد الشحن',
  2: 'وصلت',
  3: 'قيد المراجعة',
  4: 'روجعت التكلفة',
  5: 'معتمدة',
  6: 'في المستودع',
  7: 'مغلقة',
  8: 'مؤرشفة',
  9: 'ملغاة'
} as const;

export const landingCostStatusLabels = {
  0: 'مسودة',
  1: 'مراجعة',
  2: 'معتمدة'
} as const;

export const chinaContainerStatusOptions = [
  { value: 0, label: chinaContainerStatusLabels[0] },
  { value: 1, label: chinaContainerStatusLabels[1] },
  { value: 2, label: chinaContainerStatusLabels[2] },
  { value: 3, label: chinaContainerStatusLabels[3] },
  { value: 4, label: chinaContainerStatusLabels[4] },
  { value: 5, label: chinaContainerStatusLabels[5] },
  { value: 6, label: chinaContainerStatusLabels[6] },
  { value: 7, label: chinaContainerStatusLabels[7] },
  { value: 8, label: chinaContainerStatusLabels[8] },
  { value: 9, label: chinaContainerStatusLabels[9] }
] as const;

type ContainerStatusValue = keyof typeof chinaContainerStatusLabels;

export function getChinaContainerStatusTone(status: ContainerStatusValue) {
  if (status === 6) {
    return 'green';
  }
  if (status === 4 || status === 5) {
    return 'blue';
  }
  if (status === 1 || status === 2 || status === 3) {
    return 'amber';
  }
  if (status === 9) {
    return 'red';
  }
  return 'gray';
}
