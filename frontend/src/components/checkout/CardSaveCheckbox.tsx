import React from 'react';

export interface CardSaveCheckboxProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
}

export const CardSaveCheckbox: React.FC<CardSaveCheckboxProps> = ({ checked, onChange }) => {
  return (
    <label className="flex items-start gap-3 cursor-pointer select-none text-xs text-dp-blue/80 py-1">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="mt-0.5 w-4 h-4 rounded text-dp-blue border-dp-line focus:ring-dp-pink cursor-pointer"
      />
      <span>
        <strong className="text-dp-blue">Сохранить карту</strong> для безопасных покупок в один клик в будущем
      </span>
    </label>
  );
};
