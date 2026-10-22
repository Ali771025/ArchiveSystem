// src/components/features/DynamicFormBuilder.tsx (Complete Implementation)
import React, { useState, useEffect } from 'react';
import { Input } from '../../components/common/Input';
import { Button } from '../../components/common/Button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../components/ui/select';
import { Checkbox } from '../../components/ui/checkbox';
import { DatePicker } from '../../components/ui/date-picker';
import { Label } from '../../components/ui/label';

interface DynamicFormProps {
  formDefinition: any; // Should be FormDefinition type
  onSubmit: (formData: any) => void;
  onCancel?: () => void;
  initialData?: Record<string, any>;
}

const DynamicFormBuilder: React.FC<DynamicFormProps> = ({ formDefinition, onSubmit, onCancel, initialData = {} }) => {
  const [formData, setFormData] = useState<Record<string, any>>(initialData);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    // Initialize form data with default values
    const initialDataCopy: Record<string, any> = { ...initialData };
    formDefinition.groups.forEach((group: any) => {
      group.fields.forEach((field: any) => {
        if (field.defaultValue && !initialDataCopy.hasOwnProperty(field.name)) {
          initialDataCopy[field.name] = field.defaultValue;
        }
      });
    });
    setFormData(initialDataCopy);
  }, [formDefinition, initialData]);

  const handleChange = (fieldName: string, value: any) => {
    setFormData(prev => ({
      ...prev,
      [fieldName]: value
    }));
    
    // Clear error when field changes
    if (errors[fieldName]) {
      setErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[fieldName];
        return newErrors;
      });
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    formDefinition.groups.forEach((group: any) => {
      group.fields.forEach((field: any) => {
        const value = formData[field.name];

        // Check required fields
        if (field.required && (!value || value === '')) {
          newErrors[field.name] = `${field.label} is required`;
        }

        // Validate using regex if provided
        if (field.validationRegex && value && value !== '') {
          const regex = new RegExp(field.validationRegex);
          if (!regex.test(value)) {
            newErrors[field.name] = `${field.label} does not match required format`;
          }
        }
      });
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  const renderField = (field: any) => {
    const commonProps = {
      id: field.name,
      value: formData[field.name] || '',
      onChange: (e: React.ChangeEvent<HTMLInputElement>) => handleChange(field.name, e.target.value),
      className: errors[field.name] ? 'border-red-500' : ''
    };

    switch (field.type) {
      case 'text':
        return (
          <div className="space-y-2">
            <Label htmlFor={field.name}>{field.label}</Label>
            <Input 
              {...commonProps} 
              placeholder={field.placeholder}
              disabled={field.readOnly}
            />
            {errors[field.name] && <p className="text-red-500 text-sm">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
          </div>
        );
      
      case 'number':
        return (
          <div className="space-y-2">
            <Label htmlFor={field.name}>{field.label}</Label>
            <Input 
              type="number" 
              {...commonProps} 
              placeholder={field.placeholder}
              disabled={field.readOnly}
            />
            {errors[field.name] && <p className="text-red-500 text-sm">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
          </div>
        );
      
      case 'dropdown':
        return (
          <div className="space-y-2">
            <Label htmlFor={field.name}>{field.label}</Label>
            <Select 
              value={formData[field.name] || ''} 
              onValueChange={(value) => handleChange(field.name, value)}
              disabled={field.readOnly}
            >
              <SelectTrigger className={errors[field.name] ? 'border-red-500' : ''}>
                <SelectValue placeholder={field.placeholder || `Select ${field.label}`} />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(field.options).map(([key, value]) => (
                  <SelectItem key={key} value={key}>{value as string}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors[field.name] && <p className="text-red-500 text-sm">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
          </div>
        );
      
      case 'checkbox':
        return (
          <div className="flex items-center gap-2">
            <Checkbox
              id={field.name}
              checked={!!formData[field.name]}
              onCheckedChange={(checked) => handleChange(field.name, checked)}
              disabled={field.readOnly}
            />
            <Label htmlFor={field.name}>{field.label}</Label>
            {errors[field.name] && <p className="text-red-500 text-sm mr-2">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground mr-2">{field.helpText}</p>}
          </div>
        );
      
      case 'date':
        return (
          <div className="space-y-2">
            <Label htmlFor={field.name}>{field.label}</Label>
            <DatePicker
              selected={formData[field.name] ? new Date(formData[field.name]) : null}
              onChange={(date) => handleChange(field.name, date)}
              className={errors[field.name] ? 'border-red-500' : ''}
              disabled={field.readOnly}
            />
            {errors[field.name] && <p className="text-red-500 text-sm">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
          </div>
        );
      
      case 'textarea':
        return (
          <div className="space-y-2">
            <Label htmlFor={field.name}>{field.label}</Label>
            <textarea
              id={field.name}
              value={formData[field.name] || ''}
              onChange={(e) => handleChange(field.name, e.target.value)}
              placeholder={field.placeholder}
              className={`w-full p-2 border rounded ${errors[field.name] ? 'border-red-500' : ''}`}
              disabled={field.readOnly}
              rows={field.rows || 4}
            />
            {errors[field.name] && <p className="text-red-500 text-sm">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
          </div>
        );
      
      case 'email':
        return (
          <div className="space-y-2">
            <Label htmlFor={field.name}>{field.label}</Label>
            <Input 
              type="email" 
              {...commonProps} 
              placeholder={field.placeholder}
              disabled={field.readOnly}
            />
            {errors[field.name] && <p className="text-red-500 text-sm">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
          </div>
        );
      
      case 'phone':
        return (
          <div className="space-y-2">
            <Label htmlFor={field.name}>{field.label}</Label>
            <Input 
              type="tel" 
              {...commonProps} 
              placeholder={field.placeholder}
              disabled={field.readOnly}
            />
            {errors[field.name] && <p className="text-red-500 text-sm">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
          </div>
        );
      
      default:
        return (
          <div className="space-y-2">
            <Label htmlFor={field.name}>{field.label}</Label>
            <Input 
              {...commonProps} 
              placeholder={field.placeholder}
              disabled={field.readOnly}
            />
            {errors[field.name] && <p className="text-red-500 text-sm">{errors[field.name]}</p>}
            {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
          </div>
        );
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {formDefinition.groups.map((group: any, groupIndex: number) => (
        <div key={groupIndex} className="border rounded-lg p-4">
          <h3 className="font-semibold mb-4">{group.name}</h3>
          <p className="text-sm text-muted-foreground mb-4">{group.description}</p>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {group.fields.map((field: any, fieldIndex: number) => (
              <div key={fieldIndex}>
                {renderField(field)}
              </div>
            ))}
          </div>
        </div>
      ))}
      
      <div className="flex justify-end gap-2">
        {onCancel && (
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
        )}
        <Button type="submit">Submit</Button>
      </div>
    </form>
  );
};

export default DynamicFormBuilder;
