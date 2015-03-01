-- phpMyAdmin SQL Dump
-- version 3.2.4
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Mar 01, 2015 at 12:49 PM
-- Server version: 5.1.41
-- PHP Version: 5.3.1

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `metaexchange`
--

-- --------------------------------------------------------

--
-- Table structure for table `fee_collections`
--

CREATE TABLE IF NOT EXISTS `fee_collections` (
  `uid` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `symbol_pair` varchar(20) NOT NULL,
  `buy_trxid` varchar(64) DEFAULT NULL,
  `sell_trxid` varchar(64) DEFAULT NULL,
  `buy_fee` decimal(18,8) NOT NULL,
  `sell_fee` decimal(18,8) NOT NULL,
  `date` datetime NOT NULL,
  `exception` text,
  `transaction_processed_uid` int(10) unsigned NOT NULL,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=3 ;

-- --------------------------------------------------------

--
-- Table structure for table `general_exceptions`
--

CREATE TABLE IF NOT EXISTS `general_exceptions` (
  `hash` int(10) unsigned NOT NULL,
  `message` text COLLATE utf8_unicode_ci NOT NULL,
  `date` datetime NOT NULL,
  `count` int(11) NOT NULL DEFAULT '0',
  UNIQUE KEY `hash` (`hash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ignored`
--

CREATE TABLE IF NOT EXISTS `ignored` (
  `uid` int(11) NOT NULL AUTO_INCREMENT,
  `txid` varchar(64) NOT NULL,
  `notes` text,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Table structure for table `markets`
--

CREATE TABLE IF NOT EXISTS `markets` (
  `symbol_pair` varchar(20) NOT NULL,
  `ask` decimal(18,8) NOT NULL,
  `bid` decimal(18,8) NOT NULL,
  `ask_max` decimal(18,8) NOT NULL COMMENT 'quantity of BTC',
  `bid_max` decimal(18,8) NOT NULL COMMENT 'quantity of bitAsset',
  `ask_fee_percent` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `bid_fee_percent` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `transaction_processed_uid` int(10) unsigned NOT NULL DEFAULT '0',
  `spread_percent` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `window_percent` decimal(18,8) NOT NULL DEFAULT '10.00000000',
  `price_discovery` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`symbol_pair`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `sender_to_deposit`
--

CREATE TABLE IF NOT EXISTS `sender_to_deposit` (
  `receiving_address` varchar(64) NOT NULL,
  `deposit_address` varchar(36) NOT NULL,
  `symbol_pair` varchar(20) NOT NULL DEFAULT 'bitBTC_BTC',
  PRIMARY KEY (`receiving_address`,`symbol_pair`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `stats`
--

CREATE TABLE IF NOT EXISTS `stats` (
  `last_bitshares_block` int(10) unsigned NOT NULL DEFAULT '0',
  `last_bitcoin_block` varchar(64) DEFAULT NULL,
  `uid` int(11) NOT NULL AUTO_INCREMENT,
  `site_last_tid` int(10) unsigned NOT NULL DEFAULT '0',
  `bitcoin_withdraw_address` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=4 ;

-- --------------------------------------------------------

--
-- Table structure for table `transactions`
--

CREATE TABLE IF NOT EXISTS `transactions` (
  `uid` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `received_txid` varchar(64) NOT NULL,
  `sent_txid` varchar(64) DEFAULT NULL,
  `symbol_pair` varchar(20) NOT NULL,
  `order_type` enum('none','buy','sell') NOT NULL,
  `amount` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `price` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `fee` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `date` datetime NOT NULL,
  `notes` varchar(64) DEFAULT NULL,
  `status` enum('none','processing','completed','refunded') NOT NULL,
  `deposit_address` varchar(36) DEFAULT NULL,
  PRIMARY KEY (`uid`),
  UNIQUE KEY `received_txid` (`received_txid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=141 ;

-- --------------------------------------------------------

--
-- Table structure for table `withdrawals`
--

CREATE TABLE IF NOT EXISTS `withdrawals` (
  `received_txid` varchar(64) NOT NULL,
  `sent_txid` varchar(64) NOT NULL,
  `amount` decimal(18,8) NOT NULL,
  `to_account` varchar(64) NOT NULL,
  `date` datetime NOT NULL,
  `symbol` varchar(10) NOT NULL,
  PRIMARY KEY (`received_txid`),
  UNIQUE KEY `txid` (`sent_txid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
