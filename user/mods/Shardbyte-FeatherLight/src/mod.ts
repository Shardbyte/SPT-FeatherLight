import { DependencyContainer } from "tsyringe";

import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import { ItemHelper } from "@spt/helpers/ItemHelper";
import { BaseClasses } from "@spt/models/enums/BaseClasses";
import { VFS } from "@spt/utils/VFS";
import { jsonc } from "jsonc";
import path from "path";


class FeatherLight implements IPostDBLoadMod 
{

    private modConfig;
    public postDBLoad(container: DependencyContainer): void

    {
        const vfs = container.resolve<VFS>("VFS");
        this.modConfig = jsonc.parse(vfs.readFile(path.resolve(__dirname, "../config/config.jsonc")));

        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        const tables: IDatabaseTables = databaseServer.getTables();
        const itemHelper: ItemHelper = container.resolve<ItemHelper>("ItemHelper");
        const items = Object.values(tables.templates.items);

        // -------------------- Ammos --------------------

        if (this.modConfig.lightAmmo.enabled) {
        // Base Class: 5485a8684bdc2da71d8b4567
            const ammos = items.filter(x => itemHelper.isOfBaseclass(x._id, BaseClasses.AMMO));

            for (const itemAmmo of ammos)
            {
                if (itemAmmo._props.Weight)
                {
                // Set its weight to 0
                    itemAmmo._props.Weight = 0;
                }
            }
        }

        // -------------------- Throwables --------------------

        if (this.modConfig.lightThrowables.enabled) {
        // Base Class: 5485a8684bdc2da71d8b4567
            const throwables = items.filter(x => itemHelper.isOfBaseclass(x._id, BaseClasses.THROW_WEAPON));

            for (const itemThrowables of throwables)
            {
                if (itemThrowables._props.Weight)
                {
                // Set its weight to 0
                    itemThrowables._props.Weight = 0;
                }
            }
        }

        // -------------------- Foods --------------------

        if (this.modConfig.lightFood.enabled) {
        // Base Class: 5448e8d04bdc2ddf718b4569
            const foods = items.filter(x => itemHelper.isOfBaseclass(x._id, BaseClasses.FOOD));

            for (const itemFood of foods)
            {
                if (itemFood._props.Weight)
                {
                // Set its weight to 0
                    itemFood._props.Weight = 0;
                }
            }
        }

        // -------------------- Drinks --------------------

        if (this.modConfig.lightDrink.enabled) {
        // Base Class: 543be6674bdc2df1348b4569
            const drinks = items.filter(x => itemHelper.isOfBaseclass(x._id, BaseClasses.FOOD_DRINK));

            for (const itemDrink of drinks)
            {
                if (itemDrink._props.Weight)
                {
                // Set its weight to 0
                    itemDrink._props.Weight = 0;
                }
            }
        }

        // -------------------- Meds --------------------

        if (this.modConfig.lightMeds.enabled) {
        // Base Class Meds:
            const meds = items.filter(x => itemHelper.isOfBaseclass(x._id, BaseClasses.MEDS));

            for (const itemMeds of meds)
            {
                if (itemMeds._props.Weight)
                {
                // Set its weight to 0
                    itemMeds._props.Weight = 0;
                }
            }
        }
    }
}

export const mod = new FeatherLight();