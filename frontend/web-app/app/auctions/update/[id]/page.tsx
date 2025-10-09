import Heading from "@/app/components/Heading";
import AuctionForm from "../../AuctionForm";
import { getDetaildViewData } from "@/app/actions/auctionActions";

export default async function Update({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const data = await getDetaildViewData(id);

  return (
    <div className="mx-auto max-w-[75%] shadow-lg p-10 bg-white rounded-lg">
      <Heading
        title="Update your Auction"
        subtitle="Please enter the details below"
      />
      <AuctionForm auction={data} />
    </div>
  );
}
