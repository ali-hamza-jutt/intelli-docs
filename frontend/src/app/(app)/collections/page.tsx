"use client";

import Link from "next/link";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Tile } from "@/components/ui/Tile";
import { COLLECTIONS } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

export default function CollectionsPage() {
  const toast = useToast();

  return (
    <div className="page">
      <div className="flex flex-wrap items-start justify-between gap-3.5">
        <div>
          <h2 className="page-title">Collections</h2>
          <p className="page-subtitle">Group related documents so answers stay on topic.</p>
        </div>
        <Button variant="secondary" icon="plus" onClick={() => toast("Collection created")}>
          New collection
        </Button>
      </div>

      <div className="grid-fit-md mt-5.5">
        {COLLECTIONS.map((collection) => (
          <Card key={collection.id} hoverable>
            <Tile
              icon="folder"
              tone={collection.featured ? "brand" : "neutral"}
              className="size-9 rounded-[10px] text-[17px]"
            />
            <h3 className="mt-3.5 mb-1 text-md font-semibold">{collection.name}</h3>
            <p className="m-0 text-caption text-subtle">
              {collection.count} documents · updated {collection.updated}
            </p>
            <Link href="/documents" className="btn btn-secondary btn-md mt-4 w-full">
              Open
            </Link>
          </Card>
        ))}
      </div>
    </div>
  );
}
