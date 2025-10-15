"use client";

import { useAuctionStore } from "@/hooks/useAuctionStore";
import { useBidStore } from "@/hooks/useBidStore";
import { Auction, AuctionFinished, Bid } from "@/types";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { useParams } from "next/navigation";
import { ReactNode, useCallback, useEffect, useRef } from "react";
import toast from "react-hot-toast";
import AuctionCreatedToast from "../components/AuctionCreatedToast";
import { getDetaildViewData } from "../actions/auctionActions";
import AuctionFinishedToast from "../components/AuctionFinishedToast";
import { useSession } from "next-auth/react";

type Props = {
  children: ReactNode;
};

export default function SIgnalRProvider({ children }: Props) {
  const session = useSession();
  const user = session.data?.user;

  // useRef holds a mutable value that persists across re-renders
  // it stores your SignalR connection instance
  // With useRef, the object remains stable and isn’t recreated
  const connection = useRef<HubConnection | null>(null);
  const setCurrentPrice = useAuctionStore((state) => state.setCurrentPrice);
  const addBid = useBidStore((state) => state.addBid);
  const params = useParams<{ id: string }>();

  const handleAuctionFinished = useCallback(
    (finishedAuction: AuctionFinished) => {
      const auction: Promise<Auction> = getDetaildViewData(
        finishedAuction.auctionId
      );

      // toast.promise(promise, messages, options) automatically:
      //   - Shows a loading toast while the Promise is pending,
      //   - Replaces it with a success toast when the Promise resolves,
      //   - Or shows an error toast when the Promise rejects.
      // Here: It shows a toast message while auction (the Promise) is loading.
      //       When it resolves, it displays a success UI (React component).
      //       If it fails, it shows an error message.
      return toast.promise(
        auction,
        {
          // messages config
          // Message shown while the Promise is pending
          loading: "Loading",
          // Rendered when the Promise resolves successfully
          success: (auction) => (
            <AuctionFinishedToast
              finishedAuction={finishedAuction}
              auction={auction}
            />
          ),
          // Rendered when the Promise rejects
          error: () => "Auction finished",
        },
        {
          // options config
          // customizes the success toast behavior
          success: {
            duration: 10000, // The success toast stays visible for 10 seconds
            icon: null, // Removes the default success icon
          },
        }
      );
    },
    []
  );

  const handleAuctionCreated = useCallback(
    (auction: Auction) => {
      if (user?.username !== auction.seller) {
        return toast(<AuctionCreatedToast auction={auction} />, {
          duration: 10000,
        });
      }
    },
    [user]
  );

  // useCallback(...) memoizes the function so React doesn’t recreate it on every render (important for event subscription stability)
  const handleBidPlaced = useCallback(
    (bid: Bid) => {
      if (bid.bidStatus.includes("Accepted")) {
        setCurrentPrice(bid.auctionId, bid.amount);
      }

      // If user is currently viewing that specific auction (params.id matches),
      // append the bid to bid history via Zustand
      if (params.id === bid.auctionId) {
        addBid(bid);
      }
    },
    [setCurrentPrice, addBid, params.id]
  );

  useEffect(() => {
    if (!connection.current) {
      connection.current = new HubConnectionBuilder() // Uses builder pattern to create a new SignalR connection
        .withUrl(process.env.NEXT_PUBLIC_NOTIFY_URL!) // 6001 is the port of Gateway
        .withAutomaticReconnect() // Enables automatic reconnection if the connection drops (e.g., network failure).
        .build(); // Finalizes the setup and returns a HubConnection object

      connection.current
        .start() // opens the connection to the server
        .then(() => console.log("Connection started"))
        .catch((err) => console.log("Error while starting connection: " + err));
    }

    // Subscribes to the "BidPlaced" event emitted by the backend hub
    // Whenever the backend calls: await Clients.All.SendAsync("BidPlaced", bid);
    // the handleBidPlaced callback runs with the Bid payload
    connection.current.on("BidPlaced", handleBidPlaced);

    connection.current.on("AuctionCreated", handleAuctionCreated);

    connection.current.on("AuctionFinished", handleAuctionFinished);

    // Cleanup Function
    // When the component unmounts (or dependencies change), this unsubscribes the handler.
    // Prevents duplicate event bindings or memory leaks if the effect runs again
    return () => {
      connection.current?.off("BidPlaced", handleBidPlaced);
      connection.current?.off("AuctionCreated", handleAuctionCreated);
      connection.current?.off("AuctionFinished", handleAuctionFinished);
    };
  }, [
    setCurrentPrice,
    handleBidPlaced,
    handleAuctionCreated,
    handleAuctionFinished,
  ]);

  return children;
}

/**
 * Flow:
[Backend: NotificationService]
      ↓ (broadcast via SignalR)
"BidPlaced" event ----> { bid object }
      ↓
[Frontend: SignalRProvider]
      ↓
handleBidPlaced(bid)
 ├─> setCurrentPrice(bid.auctionId, bid.amount)
 └─> addBid(bid)
      ↓
[Zustand Stores Updated]
      ↓
React components re-render (UI shows new bid and updated price)
 */
