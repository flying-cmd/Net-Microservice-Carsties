"use client";

import { placeBidForAuction } from "@/app/actions/auctionActions";
import { numberWithCommas } from "@/app/lib/numberWithComma";
import { useBidStore } from "@/hooks/useBidStore";
import { FieldValues, useForm } from "react-hook-form";
import toast from "react-hot-toast";

type Props = {
  auctionId: string;
  highBid: number;
};

export default function BidForm({ auctionId, highBid }: Props) {
  const { register, handleSubmit, reset } = useForm();
  const addBid = useBidStore((state) => state.addBid);

  function onSubmit(data: FieldValues) {
    if (data.amount <= highBid) {
      reset();
      return toast.error(
        "Bid must be at least $" + numberWithCommas(highBid + 1)
      );
    }

    placeBidForAuction(auctionId, data.amount)
      .then((bid) => {
        if (bid.error) {
          reset();
          throw bid.error;
        }
        addBid(bid);
        reset();
      })
      .catch((error) => {
        toast.error(error.message);
      });
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="flex items-center border-2 rounded-lg py-2"
    >
      <input
        {...register("amount")}
        type="number"
        className="input-custom"
        placeholder={`Enter your bid (minimum bid is $${numberWithCommas(
          highBid + 1
        )})`}
      />
    </form>
  );
}
