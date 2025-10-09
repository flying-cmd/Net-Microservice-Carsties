import { HelperText, Label, TextInput } from "flowbite-react";
import { useController, UseControllerProps } from "react-hook-form";

type Props = {
  label: string;
  type?: string;
  showLabel?: boolean;
} & UseControllerProps;

export default function Input(props: Props) {
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
      {props.showLabel && (
        <div>
          <Label htmlFor={field.name}>{props.label}</Label>
        </div>
      )}
      <TextInput
        {...props}
        {...field}
        value={field.value || ""} // ensures the input doesn’t become uncontrolled if undefined
        type={props.type || "text"}
        placeholder={props.label}
        color={
          fieldState?.error ? "failure" : !fieldState.isDirty ? "" : "success"
        }
      />
      <HelperText color="failure">{fieldState.error?.message}</HelperText>
    </div>
  );
}
