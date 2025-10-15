import EmptyFilter from "@/app/components/EmptyFilter";

export default async function SignIn({
  searchParams,
}: {
  searchParams: Promise<{ callbackUrl: string }>;
}) {
  const { callbackUrl } = await searchParams;

  return (
    <EmptyFilter
      title="You need to be logged in to do that"
      subtitle="Please log in first"
      showLogin
      callbackUrl={callbackUrl}
    />
  );
}
