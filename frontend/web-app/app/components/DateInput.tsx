import DatePicker, { DatePickerProps } from "react-datepicker";
import "react-datepicker/dist/react-datepicker.css";
import { useController, UseControllerProps } from "react-hook-form";

type Props = {
  label: string;
  type?: string;
} & UseControllerProps &
  DatePickerProps;

export default function DateInput(props: Props) {
  // React Hook Form uses the control object to:
  // 1. Register the field (make) into the form state.
  // 2. Track its value, isDirty, isTouched, and error.
  // 3. Validate it using your rules.
  // 4. Return:
  //    field: props you spread into your input (value, onChange, etc.)
  //    fieldState: metadata like error, isDirty, etc.
  const { field, fieldState } = useController({ ...props });

  return (
    <div className="mb-3 block">
      <DatePicker
        {...props}
        {...field}
        selected={field.value}
        placeholderText={props.label}
        className={`
                rounded-lg
                w-full
                border 
                border-gray-600
                p-2
                flex flex-col
                ${
                  fieldState.error
                    ? "bg-red-50 border-red-500 text-red-900"
                    : !fieldState.invalid && fieldState.isDirty
                    ? "bg-green-50 border-green-500 text-green-900"
                    : ""
                }
            `}
      />
      {fieldState.error && (
        <div className="text-red-500 text-sm">{fieldState.error.message}</div>
      )}
    </div>
  );
}
