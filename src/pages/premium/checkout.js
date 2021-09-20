import React from "react";
import Load from "../../components/load";
import { get, post } from "../../api/auth";
import { withRouter } from "react-router";

class CustomerPortal extends React.Component{
	constructor(props){
		super(props);
		this.currency = this.props.match.params.currency;
		this.slots = parseInt(this.props.match.params.slots);
		this.stripe = process.env.NODE_ENV == "production"
			? window.Stripe("pk_live_51Hcvk4B8DUEVWcSDwjMf0bvWv4NiSZizxfj495VdwB3UvqPZCNYt30781RdZ4tG8QnylVc98ywuj7k13wAec6cCq00I21LkJCn")
			: window.Stripe("pk_test_51Hcvk4B8DUEVWcSDhAutXkeJErW0lmmZvTahVkIxQij2cNun9JXuh3FfIt2QXlOQVO519maTYUn8V0tcT4fnuvMH000mz5kD2V")
	}
	
	render(){
		return(
			<>
				<h1>Redirecting...</h1>
				<Load loaded={false}/>
			</>
		);
		
	}

	async componentDidMount(){
		var priceId = this.getPriceId(this.currency, this.slots);
		var response = await post("stripe/create-checkout-session", { priceId: priceId });
		var responseJson = await response.json();
		await this.stripe.redirectToCheckout({sessionId: responseJson.sessionId});
	}

	getPriceId(currency, slots){
		switch(process.env.NODE_ENV){
			case "production":
				return this.getLivePriceId(currency, slots);
			case "development":
			case "test":
				return this.getTestPriceId(currency, slots);
		}
	}

	getLivePriceId(currency, slots){
		switch (slots) {
			case 1:
				if (currency === "gbp") return "price_1I9tE0B8DUEVWcSDmXZo0tHg";
				if (currency === "usd") return "price_1I9tE0B8DUEVWcSDS7A4O1Yo";
				if (currency === "eur") return "price_1I9tE0B8DUEVWcSDt0axJ6Jy";
				break;
	
			case 3:
				if (currency === "gbp") return "price_1I9tFEB8DUEVWcSDeiZ30gkH";
				if (currency === "usd") return "price_1I9tFEB8DUEVWcSDdGJrifkV";
				if (currency === "eur") return "price_1I9tFEB8DUEVWcSDyTu8P9hn";
				break;
	
			case 5:
				if (currency === "gbp") return "price_1I9tJiB8DUEVWcSD23Zy6gBD";
				if (currency === "usd") return "price_1I9tJiB8DUEVWcSD7pIVqJJ3";
				if (currency === "eur") return "price_1I9tJiB8DUEVWcSDGKA09sOM";
				break;

			default:
				return null;
		}
	}

	getTestPriceId(currency, slots){
		switch (slots) {
			case 1:
				if (currency === "gbp") return "price_1I0bUCB8DUEVWcSDYIEsUvWA";
				if (currency === "usd") return "price_1I0bUeB8DUEVWcSD6ksIYGra";
				if (currency === "eur") return "price_1I0bWFB8DUEVWcSDut7TNTEJ";
				break;
	
			case 3:
				if (currency === "gbp") return "price_1I0bb9B8DUEVWcSDduM316AQ";
				if (currency === "usd") return "price_1I0bcoB8DUEVWcSDuzcORFfl";
				if (currency === "eur") return "price_1I0bdNB8DUEVWcSDpBaFzZMm";
				break;
	
			case 5:
				if (currency === "gbp") return "price_1I0bcOB8DUEVWcSDgqZAKvV1";
				if (currency === "usd") return "price_1I0cR7B8DUEVWcSDnikkbgGr";
				if (currency === "eur") return "price_1I0beqB8DUEVWcSDJaeyovfY";
				break;

			default:
				return null;
		}
	}
}

export default withRouter(CustomerPortal);
