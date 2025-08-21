export interface TableColumn {
  name: string;
  required?: boolean;
  label: string;
  align: 'left' | 'center' | 'right';
  field: string;
  sortable?: boolean;
  format?: (val: any) => string;
}

export interface StatusConfig {
  label: string;
  color: string;
  textColor: string;
  icon: string;
}
